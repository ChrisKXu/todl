using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.Evaluation
{
    /// <summary>
    /// An evaluator evaluates expressions and statements and give out results as output
    /// </summary>
    public class Evaluator
    {
        private readonly Binder binder = new(BoundScope.GlobalScope, BinderFlags.AllowVariableDeclarationInAssignment);
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

        private object EvaluateBoundExpression(BoundExpression boundExpression)
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

        private object EvaluateBoundUnaryExpression(BoundUnaryExpression boundUnaryExpression)
        {
            var operandValue = this.EvaluateBoundExpression(boundUnaryExpression.Operand);

            Debug.Assert(operandValue != null);

            return boundUnaryExpression.Operator.BoundUnaryOperatorKind switch
            {
                BoundUnaryExpression.BoundUnaryOperatorKind.Identity => operandValue,
                BoundUnaryExpression.BoundUnaryOperatorKind.Negation => -(int)operandValue,
                BoundUnaryExpression.BoundUnaryOperatorKind.LogicalNegation => !(bool)operandValue,
                _ => this.EvaluateBoundUnaryExpressionWithSideEffects(boundUnaryExpression)
            };
        }

        private object EvaluateBoundUnaryExpressionWithSideEffects(BoundUnaryExpression boundUnaryExpression)
        {
            if (boundUnaryExpression.Operand is BoundVariableExpression boundVariableExpression)
            {
                var variable = boundVariableExpression.Variable;
                var oldValue = this.variables[variable];

                switch (boundUnaryExpression.Operator.BoundUnaryOperatorKind)
                {
                    case BoundUnaryExpression.BoundUnaryOperatorKind.PreIncrement:
                        return SetVariableValue(variable, (int)oldValue + 1);
                    case BoundUnaryExpression.BoundUnaryOperatorKind.PostIncrement:
                        SetVariableValue(variable, (int)oldValue + 1);
                        return oldValue;
                    case BoundUnaryExpression.BoundUnaryOperatorKind.PreDecrement:
                        return SetVariableValue(variable, (int)oldValue - 1);
                    case BoundUnaryExpression.BoundUnaryOperatorKind.PostDecrement:
                        SetVariableValue(variable, (int)oldValue - 1);
                        return oldValue;
                }
            }

            return null;
        }

        private object EvaluateBoundBinaryExpression(BoundBinaryExpression boundBinaryExpression)
        {
            var leftValue = this.EvaluateBoundExpression(boundBinaryExpression.Left);
            var rightValue = this.EvaluateBoundExpression(boundBinaryExpression.Right);

            Debug.Assert(leftValue != null && rightValue != null);

            return boundBinaryExpression.Operator.BoundBinaryOperatorKind switch
            {
                BoundBinaryExpression.BoundBinaryOperatorKind.NumericAddition => (int)leftValue + (int)rightValue,
                BoundBinaryExpression.BoundBinaryOperatorKind.NumericSubstraction => (int)leftValue - (int)rightValue,
                BoundBinaryExpression.BoundBinaryOperatorKind.NumericMultiplication => (int)leftValue * (int)rightValue,
                BoundBinaryExpression.BoundBinaryOperatorKind.NumericDivision => (int)leftValue / (int)rightValue,
                BoundBinaryExpression.BoundBinaryOperatorKind.LogicalAnd => (bool)leftValue && (bool)rightValue,
                BoundBinaryExpression.BoundBinaryOperatorKind.LogicalOr => (bool)leftValue || (bool)rightValue,
                BoundBinaryExpression.BoundBinaryOperatorKind.StringConcatenation => (string)leftValue + (string)rightValue,
                _ => null
            };
        }

        private object EvaluateBoundAssignmentExpression(BoundAssignmentExpression boundAssignmentExpression)
        {
            var result = this.EvaluateBoundExpression(boundAssignmentExpression.BoundExpression);
            var variable = boundAssignmentExpression.Variable;

            return boundAssignmentExpression.Operator.BoundAssignmentOperatorKind switch
            {
                BoundAssignmentExpression.BoundAssignmentOperatorKind.Assignment => SetVariableValue(variable, result),
                BoundAssignmentExpression.BoundAssignmentOperatorKind.AdditionInline => SetVariableValue(variable, (int)this.variables[variable] + (int)result),
                BoundAssignmentExpression.BoundAssignmentOperatorKind.SubstractionInline => SetVariableValue(variable, (int)this.variables[variable] - (int)result),
                BoundAssignmentExpression.BoundAssignmentOperatorKind.MultiplicationInline => SetVariableValue(variable, (int)this.variables[variable] * (int)result),
                BoundAssignmentExpression.BoundAssignmentOperatorKind.DivisionInline => SetVariableValue(variable, (int)this.variables[variable] / (int)result),
                _ => null
            };
        }

        private object EvaluateBoundVariableExpression(BoundVariableExpression boundVariableExpression)
        {
            if (this.variables.ContainsKey(boundVariableExpression.Variable))
            {
                return this.variables[boundVariableExpression.Variable];
            }

            return null;
        }

        private object SetVariableValue(VariableSymbol variable, object value)
            => this.variables[variable] = value;
    }
}
