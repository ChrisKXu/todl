using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    /// <summary>
    /// Binder reorganizes SyntaxTree elements into Bound counterparts
    /// and prepares necessary information for Emitter to use in the emit process
    /// </summary>
    public sealed class Binder
    {
        internal BoundExpression BindExpression(Expression expression)
        {
            return expression switch
            {
                LiteralExpression literalExpression => this.BindLiteralExpression(literalExpression),
                BinaryExpression binaryExpression => this.BindBinaryExpression(binaryExpression),
                UnaryExpression unaryExpression => this.BindUnaryExpression(unaryExpression),
                ParethesizedExpression parethesizedExpression => this.BindExpression(parethesizedExpression.InnerExpression),
                _ => throw new NotImplementedException()
            };
        }

        private BoundConstant BindLiteralExpression(LiteralExpression literalExpression)
        {
            return literalExpression.LiteralToken.Kind switch
            {
                SyntaxKind.NumberToken => this.BindNumericConstant(literalExpression.LiteralToken),
                SyntaxKind.StringToken => this.BindStringConstant(literalExpression.LiteralToken),
                SyntaxKind.TrueKeywordToken or SyntaxKind.FalseKeywordToken => new BoundConstant(TypeSymbol.ClrBoolean, bool.Parse(literalExpression.LiteralToken.Text.ToReadOnlyTextSpan())),
                _ => throw new NotSupportedException($"Literal value {literalExpression.LiteralToken.Text} is not supported"),
            };
        }

        private BoundConstant BindNumericConstant(SyntaxToken syntaxToken)
        {
            var text = syntaxToken.Text.ToReadOnlyTextSpan();

            if (int.TryParse(text, out var parsedInt))
            {
                return new BoundConstant(TypeSymbol.ClrInt32, parsedInt);
            }

            if (double.TryParse(text, out var parsedDouble))
            {
                return new BoundConstant(TypeSymbol.ClrDouble, parsedDouble);
            }

            throw new NotSupportedException($"Literal value {syntaxToken.Text} is not supported");
        }

        private BoundConstant BindStringConstant(SyntaxToken syntaxToken)
        {
            var text = syntaxToken.Text.ToReadOnlyTextSpan();
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
                            throw new NotSupportedException($"Literal value {syntaxToken.Text} is not supported");
                    }
                }
                else
                {
                    builder.Append(c);
                }
            }

            return new BoundConstant(TypeSymbol.ClrString, builder.ToString());
        }

        private BoundUnaryExpression BindUnaryExpression(UnaryExpression unaryExpression)
        {
            var boundOperand = this.BindExpression(unaryExpression.Operand);
            var boundUnaryOperator = BoundUnaryExpression.MatchUnaryOperator(
                operandResultType: boundOperand.ResultType,
                syntaxKind: unaryExpression.Operator.Kind,
                trailing: unaryExpression.Trailing);

            if (boundUnaryOperator == null)
            {
                throw new NotSupportedException($"Operator {unaryExpression.Operator.Text} is not supported on type {boundOperand.ResultType.Name}");
            }

            return new BoundUnaryExpression(boundUnaryOperator, boundOperand);
        }

        private BoundBinaryExpression BindBinaryExpression(BinaryExpression binaryExpression)
        {
            var boundLeft = this.BindExpression(binaryExpression.Left);
            var boundRight = this.BindExpression(binaryExpression.Right);
            var boundBinaryOperator = BoundBinaryExpression.MatchBinaryOperator(boundLeft.ResultType, boundRight.ResultType, binaryExpression.Operator.Kind);

            if (boundBinaryOperator == null)
            {
                throw new NotSupportedException($"Operator {binaryExpression.Operator.Text} is not supported on types {boundLeft.ResultType.Name} and {boundRight.ResultType.Name}");
            }

            return new BoundBinaryExpression(boundBinaryOperator, boundLeft, boundRight);
        }
    }
}
