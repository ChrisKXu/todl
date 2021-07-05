using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.Evaluation
{
    /// <summary>
    /// An evaluator evaluates expressions and statements and give out results as output
    /// </summary>
    public class Evaluator
    {
        private readonly Binder binder = new(BoundScope.GlobalScope);
        private readonly Dictionary<VariableSymbol, object> variables = new();

        public EvaluatorResult Evaluate(SourceText sourceText)
        {
            var syntaxTree = new SyntaxTree(sourceText);
            var parser = new Parser(syntaxTree);
            parser.Lex();
            var expression = parser.ParseExpression();

            var diagnosticsOutput = parser.Diagnostics.Select(diagnostics => diagnostics.Message).ToList();

            if (diagnosticsOutput.Any())
            {
                return new EvaluatorResult()
                {
                    DiagnosticsOutput = diagnosticsOutput,
                    EvaluationOutput = null,
                    ResultType = null
                };
            }

            var boundExpression = this.binder.BindExpression(expression);

            return new EvaluatorResult()
            {
                DiagnosticsOutput = this.binder.Diagnostics.Select(diagnostics => diagnostics.Message).ToList(),
                EvaluationOutput = EvaluateBoundExpression(boundExpression),
                ResultType = boundExpression.ResultType
            };
        }

        public object EvaluateBoundExpression(BoundExpression boundExpression)
        {
            return boundExpression switch
            {
                BoundConstant boundConstant => boundConstant.Value,
                BoundUnaryExpression boundUnaryExpression => EvaluateBoundUnaryExpression(boundUnaryExpression),
                BoundBinaryExpression boundBinaryExpression => EvaluateBoundBinaryExpression(boundBinaryExpression),
                BoundAssignmentExpression boundAssignmentExpression => EvaluateBoundAssignmentExpression(boundAssignmentExpression),
                BoundVariableExpression boundVariableExpression => EvaluateBoundVariableExpression(boundVariableExpression),
                BoundErrorExpression => null,
                _ => throw new NotSupportedException($"{typeof(BoundExpression)} is not supported for evaluation"),
            };
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

                case BoundBinaryExpression.BoundBinaryOperatorKind.StringConcatenation:
                    return (string)leftValue + (string)rightValue;
            }

            throw new NotSupportedException($"Binary operator {boundBinaryExpression.Operator.SyntaxKind} not supported for evaluation");
        }

        public object EvaluateBoundAssignmentExpression(BoundAssignmentExpression boundAssignmentExpression)
        {
            var result = this.EvaluateBoundExpression(boundAssignmentExpression.BoundExpression);
            var variable = boundAssignmentExpression.Variable;

            switch (boundAssignmentExpression.Operator.BoundAssignmentOperatorKind)
            {
                case BoundAssignmentExpression.BoundAssignmentOperatorKind.Assignment:
                    this.variables[variable] = result;
                    break;
                case BoundAssignmentExpression.BoundAssignmentOperatorKind.AdditionInline:
                    this.variables[variable] = (int)this.variables[variable] + (int)result;
                    break;
                case BoundAssignmentExpression.BoundAssignmentOperatorKind.SubstractionInline:
                    this.variables[variable] = (int)this.variables[variable] - (int)result;
                    break;
                case BoundAssignmentExpression.BoundAssignmentOperatorKind.MultiplicationInline:
                    this.variables[variable] = (int)this.variables[variable] * (int)result;
                    break;
                case BoundAssignmentExpression.BoundAssignmentOperatorKind.DivisionInline:
                    this.variables[variable] = (int)this.variables[variable] / (int)result;
                    break;
                default:
                    break;
            }

            return this.variables[variable];
        }

        public object EvaluateBoundVariableExpression(BoundVariableExpression boundVariableExpression)
        {
            if (this.variables.ContainsKey(boundVariableExpression.Variable))
            {
                return this.variables[boundVariableExpression.Variable];
            }

            throw new Exception($"Variable {boundVariableExpression.Variable.Name} does not exist");
        }
    }
}
