using System.Text;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundConstant : BoundExpression
    {
        public object Value { get; internal init; }

        public override TypeSymbol ResultType
            => ClrTypeSymbol.MapClrType(Value.GetType());
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
                return BoundNodeFactory.CreateBoundConstant(
                    syntaxNode: literalExpression,
                    value: parsedInt);
            }

            if (double.TryParse(text, out var parsedDouble))
            {
                return BoundNodeFactory.CreateBoundConstant(
                    syntaxNode: literalExpression,
                    value: parsedDouble);
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

            return BoundNodeFactory.CreateBoundConstant(
                syntaxNode: literalExpression,
                value: builder.ToString());
        }

        private BoundConstant BindBooleanConstant(LiteralExpression literalExpression)
            => BoundNodeFactory.CreateBoundConstant(
                syntaxNode: literalExpression,
                value: literalExpression.LiteralToken.Kind == SyntaxKind.TrueKeywordToken);

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

            return BoundNodeFactory.CreateBoundConstant(
                syntaxNode: literalExpression,
                value: null,
                diagnosticBuilder: diagnosticBuilder);
        }
    }
}
