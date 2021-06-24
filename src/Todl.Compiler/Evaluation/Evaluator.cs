using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.Evaluation
{
    /// <summary>
    /// An evaluator evaluates expressions and statements and give out results as output
    /// </summary>
    public class Evaluator
    {
        private readonly SourceText sourceText;

        public Evaluator(SourceText sourceText)
        {
            this.sourceText = sourceText;
        }

        public EvaluatorResult Evaluate()
        {
            var syntaxTree = new SyntaxTree(this.sourceText);
            var parser = new Parser(syntaxTree);
            parser.Lex();
            var binaryExpression = parser.ParseBinaryExpression();

            var diagnosticsOutput = parser.Diagnostics.Select(diagnostics => diagnostics.Message).ToList();

            var binder = new Binder();
            var boundExpression = binder.BindExpression(binaryExpression);

            return new EvaluatorResult()
            {
                DiagnosticsOutput = diagnosticsOutput,
                EvaluationOutput = EvaluateBoundExpression(boundExpression)
            };
        }

        public object EvaluateBoundExpression(BoundExpression boundExpression)
        {
            switch (boundExpression)
            {
                case BoundConstant boundConstant:
                    return boundConstant.Value;
                case BoundUnaryExpression boundUnaryExpression:
                    return EvaluateBoundUnaryExpression(boundUnaryExpression);
                case BoundBinaryExpression boundBinaryExpression:
                    return EvaluateBoundBinaryExpression(boundBinaryExpression);
            }

            throw new NotSupportedException($"{typeof(BoundExpression)} is not supported for evaluation");
        }

        public object EvaluateBoundUnaryExpression(BoundUnaryExpression boundUnaryExpression)
        {
            var operandValue = this.EvaluateBoundExpression(boundUnaryExpression.Operand);

            Debug.Assert(operandValue != null);

            switch (boundUnaryExpression.Operator.BoundUnaryOperatorKind)
            {
                case BoundUnaryExpression.BoundUnaryOperatorKind.Identity:
                    return operandValue;
                case BoundUnaryExpression.BoundUnaryOperatorKind.Negation:
                    return -(int)operandValue;
                case BoundUnaryExpression.BoundUnaryOperatorKind.LogicalNegation:
                    return !(bool)operandValue;
            }

            throw new NotSupportedException($"Unary operator {boundUnaryExpression.Operator.SyntaxKind} not supported for evaluation");
        }

        public object EvaluateBoundBinaryExpression(BoundBinaryExpression boundBinaryExpression)
        {
            var leftValue = this.EvaluateBoundExpression(boundBinaryExpression.Left);
            var rightValue = this.EvaluateBoundExpression(boundBinaryExpression.Right);

            Debug.Assert(leftValue != null && rightValue != null);

            switch (boundBinaryExpression.Operator.BoundBinaryOperatorKind)
            {
                case BoundBinaryExpression.BoundBinaryOperatorKind.NumericAddition:
                    return (int)leftValue + (int)rightValue;
                case BoundBinaryExpression.BoundBinaryOperatorKind.NumericSubstraction:
                    return (int)leftValue - (int)rightValue;
                case BoundBinaryExpression.BoundBinaryOperatorKind.NumericMultiplication:
                    return (int)leftValue * (int)rightValue;
                case BoundBinaryExpression.BoundBinaryOperatorKind.NumericDivision:
                    return (int)leftValue / (int)rightValue;

                case BoundBinaryExpression.BoundBinaryOperatorKind.LogicalAnd:
                    return (bool)leftValue && (bool)rightValue;
                case BoundBinaryExpression.BoundBinaryOperatorKind.LogicalOr:
                    return (bool)leftValue || (bool)rightValue;
            }

            throw new NotSupportedException($"Binary operator {boundBinaryExpression.Operator.SyntaxKind} not supported for evaluation");
        }
    }
}
