﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Todl.Compiler.CodeAnalysis;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.Evaluation
{
    using Binder = Todl.Compiler.CodeAnalysis.Binding.Binder;

    /// <summary>
    /// An evaluator evaluates expressions and statements and give out results as output
    /// </summary>
    public sealed class Evaluator
    {
        private readonly Dictionary<VariableSymbol, object> variables = new();

        public EvaluatorResult Evaluate(SourceText sourceText)
        {
            var clrTypeCache = ClrTypeCache.FromAssemblies(new[] { Assembly.GetExecutingAssembly() });
            var expression = SyntaxTree.ParseExpression(sourceText, clrTypeCache);
            var diagnostics = expression.GetDiagnostics();

            if (diagnostics.Any())
            {
                return new()
                {
                    DiagnosticsOutput = diagnostics.Select(d => d.Message).ToList(),
                    EvaluationOutput = null,
                    ResultType = null
                };
            }

            var binder = Binder.CreateScriptBinder();
            var boundExpression = binder.BindExpression(expression);

            return new()
            {
                DiagnosticsOutput = boundExpression.GetDiagnostics().Select(d => d.Message).ToList(),
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
                BoundMemberAccessExpression boundMemberAccessExpression => EvaluateBoundMemberAccessExpression(boundMemberAccessExpression),
                BoundTypeExpression boundTypeExpression => boundTypeExpression.ResultType.Name,
                BoundClrFunctionCallExpression boundFunctionCallExpression => EvaluateBoundFunctionCallExpression(boundFunctionCallExpression),
                BoundObjectCreationExpression boundObjectCreationExpression => EvaluateBoundObjectCreationExpression(boundObjectCreationExpression),
                _ => throw new NotSupportedException($"{typeof(BoundExpression)} is not supported for evaluation"),
            };
        }

        private object EvaluateBoundUnaryExpression(BoundUnaryExpression boundUnaryExpression)
        {
            var operandValue = this.EvaluateBoundExpression(boundUnaryExpression.Operand);

            Debug.Assert(operandValue != null);

            return boundUnaryExpression.Operator.BoundUnaryOperatorKind switch
            {
                BoundUnaryOperatorKind.Identity => operandValue,
                BoundUnaryOperatorKind.Negation => -(int)operandValue,
                BoundUnaryOperatorKind.LogicalNegation => !(bool)operandValue,
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
                    case BoundUnaryOperatorKind.PreIncrement:
                        return SetVariableValue(variable, (int)oldValue + 1);
                    case BoundUnaryOperatorKind.PostIncrement:
                        SetVariableValue(variable, (int)oldValue + 1);
                        return oldValue;
                    case BoundUnaryOperatorKind.PreDecrement:
                        return SetVariableValue(variable, (int)oldValue - 1);
                    case BoundUnaryOperatorKind.PostDecrement:
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
            var result = this.EvaluateBoundExpression(boundAssignmentExpression.Right);
            var variable = (boundAssignmentExpression.Left as BoundVariableExpression).Variable;

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

        private object EvaluateBoundMemberAccessExpression(BoundMemberAccessExpression boundMemberAccessExpression)
        {
            var baseObject = EvaluateBoundExpression(boundMemberAccessExpression.BoundBaseExpression);
            var invokingObject = boundMemberAccessExpression.IsStatic ? null : baseObject;

            return boundMemberAccessExpression.MemberInfo switch
            {
                PropertyInfo property => property.GetValue(invokingObject),
                FieldInfo field => field.GetValue(invokingObject),
                _ => baseObject
            };
        }

        private object EvaluateBoundFunctionCallExpression(BoundClrFunctionCallExpression boundFunctionCallExpression)
        {
            var isStatic = boundFunctionCallExpression.MethodInfo.IsStatic;
            var invokingObject = isStatic ? null : EvaluateBoundExpression(boundFunctionCallExpression.BoundBaseExpression);

            var arguments = boundFunctionCallExpression.BoundArguments.Select(EvaluateBoundExpression).ToArray();

            // assuming the BoundMemberAccessKind is Function since it's checked in Binder
            return boundFunctionCallExpression.MethodInfo.Invoke(invokingObject, arguments);
        }

        private object EvaluateBoundObjectCreationExpression(BoundObjectCreationExpression boundObjectCreationExpression)
        {
            var arguments = boundObjectCreationExpression.BoundArguments.Select(a => EvaluateBoundExpression(a));
            return boundObjectCreationExpression.ConstructorInfo.Invoke(arguments.ToArray());
        }

        private object SetVariableValue(VariableSymbol variable, object value)
            => this.variables[variable] = value;
    }
}
