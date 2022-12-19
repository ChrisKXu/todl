using System;
using System.Linq;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;

using MethodInfo = System.Reflection.MethodInfo;

namespace Todl.Compiler.CodeGeneration;

internal partial class Emitter
{
    internal sealed partial class FunctionEmitter
    {
        // TODO: Replace this with proper lowering logic
        private static readonly MethodInfo StringConcatMethodInfo = typeof(string)
            .GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
            .Single(m => m.Name == nameof(string.Concat)
                && m.GetParameters().Length == 2
                && m.GetParameters()[0].ParameterType.Equals(typeof(string)));

        private void EmitExpression(BoundExpression boundExpression)
        {
            switch (boundExpression)
            {
                case BoundConstant boundConstant:
                    EmitConstant(boundConstant);
                    return;
                case BoundClrFunctionCallExpression boundClrFunctionCallExpression:
                    EmitClrFunctionCallExpression(boundClrFunctionCallExpression);
                    return;
                case BoundTodlFunctionCallExpression boundTodlFunctionCallExpression:
                    EmitTodlFunctionCallExpression(boundTodlFunctionCallExpression);
                    return;
                case BoundBinaryExpression boundBinaryExpression:
                    EmitBinaryExpression(boundBinaryExpression);
                    return;
                case BoundVariableExpression boundVariableExpression:
                    EmitVariableExpression(boundVariableExpression);
                    return;
                default:
                    throw new NotSupportedException($"Expression type {boundExpression.GetType().Name} is not supported.");
            }
        }

        private void EmitConstant(BoundConstant boundConstant)
        {
            if (boundConstant.ResultType.Equals(BuiltInTypes.Int32))
            {
                ilProcessor.Emit(OpCodes.Ldc_I4, (int)boundConstant.Value);
            }
            else if (boundConstant.ResultType.Equals(BuiltInTypes.String))
            {
                ilProcessor.Emit(OpCodes.Ldstr, (string)boundConstant.Value);
            }
        }

        private void EmitClrFunctionCallExpression(BoundClrFunctionCallExpression boundClrFunctionCallExpression)
        {
            foreach (var argument in boundClrFunctionCallExpression.BoundArguments)
            {
                EmitExpression(argument);
            }

            if (!boundClrFunctionCallExpression.IsStatic)
            {
                EmitExpression(boundClrFunctionCallExpression.BoundBaseExpression);
            }

            var methodReference = ResolveMethodReference(boundClrFunctionCallExpression);
            ilProcessor.Emit(OpCodes.Call, methodReference);
        }

        private void EmitBinaryExpression(BoundBinaryExpression boundBinaryExpression)
        {
            EmitExpression(boundBinaryExpression.Left);
            EmitExpression(boundBinaryExpression.Right);

            switch (boundBinaryExpression.Operator.BoundBinaryOperatorKind)
            {
                case BoundBinaryOperatorKind.Equality:
                    ilProcessor.Emit(OpCodes.Ceq);
                    return;
                case BoundBinaryOperatorKind.Comparison:
                    ilProcessor.Emit(OpCodes.Cgt);
                    return;
                case BoundBinaryOperatorKind.LogicalAnd:
                    ilProcessor.Emit(OpCodes.And);
                    return;
                case BoundBinaryOperatorKind.LogicalOr:
                    ilProcessor.Emit(OpCodes.Or);
                    return;
                case BoundBinaryOperatorKind.NumericAddition:
                    ilProcessor.Emit(OpCodes.Add);
                    return;
                case BoundBinaryOperatorKind.NumericSubstraction:
                    ilProcessor.Emit(OpCodes.Sub);
                    return;
                case BoundBinaryOperatorKind.StringConcatenation:
                    var methodReference = AssemblyDefinition.MainModule.ImportReference(StringConcatMethodInfo);
                    ilProcessor.Emit(OpCodes.Call, methodReference);
                    return;
                default:
                    return;
            }
        }

        private void EmitVariableExpression(BoundVariableExpression boundVariableExpression)
        {
            switch (boundVariableExpression.Variable)
            {
                case ParameterSymbol parameter:
                    var parameterDefinition = methodDefinition.Parameters.FirstOrDefault(p => p.Name.Equals(parameter.Name));
                    ilProcessor.Emit(OpCodes.Ldarg, parameterDefinition);
                    break;
                case LocalVariableSymbol localVariable:
                    ilProcessor.Emit(OpCodes.Ldloca_S, variables[localVariable]);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void EmitTodlFunctionCallExpression(BoundTodlFunctionCallExpression boundTodlFunctionCallExpression)
        {
            foreach (var argument in boundTodlFunctionCallExpression.BoundArguments.Values)
            {
                EmitExpression(argument);
            }

            var methodReference = ResolveMethodReference(boundTodlFunctionCallExpression);
            ilProcessor.Emit(OpCodes.Call, methodReference);
        }

    }
}
