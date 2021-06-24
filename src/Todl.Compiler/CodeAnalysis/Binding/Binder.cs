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
            switch(expression)
            {
                case LiteralExpression literalExpression:
                    return this.BindLiteralExpression(literalExpression);
                case BinaryExpression binaryExpression:
                    return this.BindBinaryExpression(binaryExpression);
                case UnaryExpression unaryExpression:
                    return this.BindUnaryExpression(unaryExpression);
                case ParethesizedExpression parethesizedExpression:
                    return this.BindExpression(parethesizedExpression.InnerExpression);
            }

            throw new NotImplementedException();
        }

        private BoundConstant BindLiteralExpression(LiteralExpression literalExpression)
        {
            switch (literalExpression.LiteralToken.Kind)
            {
                case SyntaxKind.NumberToken:
                    return this.BindNumericConstant(literalExpression.LiteralToken);
                case SyntaxKind.TrueKeywordToken:
                case SyntaxKind.FalseKeywordToken:
                    return new BoundConstant(TypeSymbol.ClrBoolean, Boolean.Parse(literalExpression.LiteralToken.Text.ToReadOnlyTextSpan()));
            }

            throw new NotSupportedException($"Literal value {literalExpression.LiteralToken.Text} is not supported");
        }

        private BoundConstant BindNumericConstant(SyntaxToken syntaxToken)
        {
            var text = syntaxToken.Text.ToReadOnlyTextSpan();

            int parsedInt;
            if (Int32.TryParse(text, out parsedInt))
            {
                return new BoundConstant(TypeSymbol.ClrInt32, parsedInt);
            }

            double parsedDouble;
            if (Double.TryParse(text, out parsedDouble))
            {
                return new BoundConstant(TypeSymbol.ClrDouble, parsedDouble);
            }

            throw new NotSupportedException($"Literal value {syntaxToken.Text} is not supported");
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
