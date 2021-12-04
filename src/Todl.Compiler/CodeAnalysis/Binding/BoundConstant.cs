using System.Text;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundConstant : BoundExpression
    {
        public object Value { get; internal init; }
    }

    public partial class Binder
    {
        private BoundExpression BindLiteralExpression(LiteralExpression literalExpression)
        {
            return literalExpression.LiteralToken.Kind switch
            {
                SyntaxKind.NumberToken => BindNumericConstant(literalExpression),
                SyntaxKind.StringToken => BindStringConstant(literalExpression),
                SyntaxKind.TrueKeywordToken or SyntaxKind.FalseKeywordToken
                    => BindBooleanConstant(literalExpression),
                _ => ReportUnsupportedLiteral(literalExpression)
            };
        }

        private BoundConstant BindNumericConstant(LiteralExpression literalExpression)
        {
            var text = literalExpression.LiteralToken.Text.ToReadOnlyTextSpan();

            if (int.TryParse(text, out var parsedInt))
            {
                return new()
                {
                    ResultType = TypeSymbol.ClrInt32,
                    Value = parsedInt,
                    SyntaxNode = literalExpression
                };
            }

            if (double.TryParse(text, out var parsedDouble))
            {
                return new()
                {
                    ResultType = TypeSymbol.ClrDouble,
                    Value = parsedDouble,
                    SyntaxNode = literalExpression
                };
            }

            return ReportUnsupportedLiteral(literalExpression);
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

            return new()
            {
                ResultType = TypeSymbol.ClrString,
                Value = builder.ToString(),
                SyntaxNode = literalExpression
            };
        }

        private BoundConstant BindBooleanConstant(LiteralExpression literalExpression)
            => new()
            {
                ResultType = TypeSymbol.ClrBoolean,
                Value = literalExpression.LiteralToken.Kind == SyntaxKind.TrueKeywordToken,
                SyntaxNode = literalExpression
            };

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

            return new()
            {
                SyntaxNode = literalExpression,
                DiagnosticBuilder = diagnosticBuilder
            };
        }
    }
}
