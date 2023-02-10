using System;
using System.Text;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public abstract class BoundConstant : BoundExpression
    {
        public virtual object Value { get; }

        public override bool Constant => true;
    }

    public sealed class BoundStringConstant : BoundConstant
    {
        public string StringValue { get; internal init; }

        public override object Value => StringValue;

        public override TypeSymbol ResultType
            => SyntaxNode.SyntaxTree.ClrTypeCache.BuiltInTypes.String;
    }

    public sealed class BoundBooleanConstant : BoundConstant
    {
        public bool BooleanValue { get; internal init; }

        public override object Value => BooleanValue;

        public override TypeSymbol ResultType
            => SyntaxNode.SyntaxTree.ClrTypeCache.BuiltInTypes.Boolean;
    }

    public sealed class BoundNullConstant : BoundConstant
    {
        public override object Value => null;
    }

    public sealed class BoundNumericConstant : BoundConstant
    {
        public object NumericValue { get; internal init; }

        public override object Value => NumericValue;

        public override TypeSymbol ResultType
            => SyntaxNode.SyntaxTree.ClrTypeCache.Resolve(Value.GetType().FullName);
    }

    public partial class Binder
    {
        private BoundExpression BindLiteralExpression(LiteralExpression literalExpression)
            => literalExpression.LiteralToken.Kind switch
            {
                SyntaxKind.NumberToken => BindNumericConstant(literalExpression),
                SyntaxKind.StringToken => BindStringConstant(literalExpression),
                SyntaxKind.TrueKeywordToken or SyntaxKind.FalseKeywordToken
                    => BindBooleanConstant(literalExpression),
                _ => ReportUnsupportedLiteral(literalExpression)
            };

        // We might want to optimize this later but this does the job for now
        private object ConvertToIntOrLong(string input, int @base)
        {
            try
            {
                return Convert.ToInt32(input, @base);
            }
            catch (OverflowException) { }

            try
            {
                return Convert.ToUInt32(input, @base);
            }
            catch (OverflowException) { }

            try
            {
                return Convert.ToInt64(input, @base);
            }
            catch (OverflowException)
            {
                return Convert.ToUInt64(input, @base);
            }
        }

        private BoundConstant BindNumericConstant(LiteralExpression literalExpression)
        {
            var text = literalExpression.LiteralToken.Text.ToReadOnlyTextSpan();

            var @base = 10;
            var startIndex = 0;

            if (text.StartsWith("0x") || text.StartsWith("0X"))
            {
                @base = 16;
                startIndex = 2;
            }
            else if (text.StartsWith("0b") || text.StartsWith("0B"))
            {
                @base = 2;
                startIndex = 2;
            }

            var value = text[^1] switch
            {
                'f' or 'F' => @base switch
                {
                    10 => float.Parse(text[0..^1]),
                    _ => ConvertToIntOrLong(text[startIndex..].ToString(), @base)
                },
                'd' or 'D' => @base switch
                {
                    10 => double.Parse(text[0..^1]),
                    _ => ConvertToIntOrLong(text[startIndex..].ToString(), @base),
                },
                'u' or 'U' => Convert.ToUInt32(text[startIndex..^1].ToString(), @base),
                'l' or 'L' => text[^2] switch
                {
                    'u' or 'U' => Convert.ToUInt64(text[startIndex..^2].ToString(), @base),
                    _ => Convert.ToInt64(text[startIndex..^1].ToString(), @base)
                },
                _ when text.Contains('.') => double.Parse(text),
                _ => ConvertToIntOrLong(text[startIndex..].ToString(), @base)
            };

            return value is not null
                ? BoundNodeFactory.CreateBoundNumericConstant(
                    syntaxNode: literalExpression,
                    numericValue: value)
                : ReportUnsupportedLiteral(literalExpression);
        }

        private BoundConstant BindStringConstant(LiteralExpression literalExpression)
        {
            var text = literalExpression.LiteralToken.Text.ToReadOnlyTextSpan();
            var escape = text[0] != '@';
            var builder = new StringBuilder();

            for (var i = escape ? 1 : 2; i < text.Length - 1; ++i)
            {
                var c = text[i];
                if (escape && c == '\\')
                {
                    switch (text[++i])
                    {
                        case 't':
                            builder.Append('\t');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case '\\':
                            builder.Append('\\');
                            break;
                        case '"':
                            builder.Append('"');
                            break;
                        default:
                            return ReportUnsupportedLiteral(literalExpression);
                    }
                }
                else
                {
                    builder.Append(c);
                }
            }

            return BoundNodeFactory.CreateBoundStringConstant(
                syntaxNode: literalExpression,
                stringValue: builder.ToString());
        }

        private BoundConstant BindBooleanConstant(LiteralExpression literalExpression)
            => BoundNodeFactory.CreateBoundBooleanConstant(
                syntaxNode: literalExpression,
                booleanValue: literalExpression.LiteralToken.Kind == SyntaxKind.TrueKeywordToken);

        private BoundConstant ReportUnsupportedLiteral(LiteralExpression literalExpression)
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            diagnosticBuilder.Add(
                new Diagnostic()
                {
                    Message = $"Literal value {literalExpression.Text} is not supported",
                    Level = DiagnosticLevel.Error,
                    TextLocation = literalExpression.LiteralToken.GetTextLocation(),
                    ErrorCode = ErrorCode.UnsupportedLiteral
                });

            return BoundNodeFactory.CreateBoundNullConstant(
                syntaxNode: literalExpression,
                diagnosticBuilder: diagnosticBuilder);
        }
    }
}
