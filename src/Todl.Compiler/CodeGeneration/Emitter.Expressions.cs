using System;
using System.Linq;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;

using MethodInfo = System.Reflection.MethodInfo;

namespace Todl.Compiler.CodeGeneration;

internal partial class Emitter
{
    internal partial class InstructionEmitter
    {
        // TODO: Replace this with proper lowering logic
        private static readonly MethodInfo StringConcatMethodInfo = typeof(string)
            .GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
            .Single(m => m.Name == nameof(string.Concat)
                && m.GetParameters().Length == 2
                && m.GetParameters()[0].ParameterType.Equals(typeof(string)));

        public void EmitExpression(BoundExpression boundExpression)
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
            switch (boundConstant)
            {
                case BoundStringConstant boundStringConstant:
                    EmitStringConstant(boundStringConstant);
                    return;
                case BoundBooleanConstant boundBooleanConstant:
                    EmitIntValue(boundBooleanConstant.BooleanValue ? 1 : 0);
                    return;
                case BoundNullConstant:
                    ILProcessor.Emit(OpCodes.Ldnull);
                    return;
            }

            var resultType = boundConstant.ResultType;

            if (resultType.Equals(BuiltInTypes.Float))
            {
                ILProcessor.Emit(OpCodes.Ldc_R4, (float)boundConstant.Value);
            }
            else if (resultType.Equals(BuiltInTypes.Double))
            {
                ILProcessor.Emit(OpCodes.Ldc_R8, (double)boundConstant.Value);
            }
            else if (resultType.Equals(BuiltInTypes.Int32) || resultType.Equals(BuiltInTypes.UInt32))
            {
                EmitInt32Constant(boundConstant);
            }
            else if (resultType.Equals(BuiltInTypes.Int64) || resultType.Equals(BuiltInTypes.UInt64))
            {
                EmitInt64Constant(boundConstant);
            }
        }

        private void EmitStringConstant(BoundStringConstant boundStringConstant)
        {
            if (boundStringConstant.Value is null)
            {
                ILProcessor.Emit(OpCodes.Ldnull);
                return;
            }

            ILProcessor.Emit(OpCodes.Ldstr, boundStringConstant.StringValue);
        }

        private void EmitIntValue(int intValue)
        {
            var opCode = intValue switch
            {
                -1 => OpCodes.Ldc_I4_M1,
                0 => OpCodes.Ldc_I4_0,
                1 => OpCodes.Ldc_I4_1,
                2 => OpCodes.Ldc_I4_2,
                3 => OpCodes.Ldc_I4_3,
                4 => OpCodes.Ldc_I4_4,
                5 => OpCodes.Ldc_I4_5,
                6 => OpCodes.Ldc_I4_6,
                7 => OpCodes.Ldc_I4_7,
                8 => OpCodes.Ldc_I4_8,
                _ => OpCodes.Nop
            };

            if (opCode != OpCodes.Nop)
            {
                ILProcessor.Emit(opCode);
                return;
            }

            if (unchecked((sbyte)intValue) == intValue)
            {
                ILProcessor.Emit(OpCodes.Ldc_I4_S, unchecked((sbyte)intValue));
            }
            else
            {
                ILProcessor.Emit(OpCodes.Ldc_I4, intValue);
            }
        }

        private void EmitInt32Constant(BoundConstant boundConstant)
        {
            var intValue = boundConstant.ResultType.Equals(BuiltInTypes.Int32)
                ? unchecked((int)boundConstant.Value)
                : unchecked((int)(uint)boundConstant.Value);

            EmitIntValue(intValue);
        }

        private void EmitInt64Constant(BoundConstant boundConstant)
        {
            var longValue = boundConstant.ResultType.Equals(BuiltInTypes.Int64)
                ? unchecked((long)boundConstant.Value)
                : unchecked((long)(ulong)boundConstant.Value);

            if (longValue >= int.MinValue && longValue <= int.MaxValue)
            {
                EmitIntValue(unchecked((int)longValue));
                ILProcessor.Emit(OpCodes.Conv_I8);
            }
            else if (longValue > int.MaxValue && longValue <= uint.MaxValue)
            {
                EmitIntValue(unchecked((int)longValue));
                ILProcessor.Emit(OpCodes.Conv_U8);
            }
            else
            {
                ILProcessor.Emit(OpCodes.Ldc_I8, longValue);
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
            ILProcessor.Emit(OpCodes.Call, methodReference);
        }

        private void EmitBinaryExpression(BoundBinaryExpression boundBinaryExpression)
        {
            EmitExpression(boundBinaryExpression.Left);
            EmitExpression(boundBinaryExpression.Right);

            switch (boundBinaryExpression.Operator.BoundBinaryOperatorKind)
            {
                case BoundBinaryOperatorKind.Equality:
                    ILProcessor.Emit(OpCodes.Ceq);
                    return;
                case BoundBinaryOperatorKind.Comparison:
                    ILProcessor.Emit(OpCodes.Cgt);
                    return;
                case BoundBinaryOperatorKind.LogicalAnd:
                    ILProcessor.Emit(OpCodes.And);
                    return;
                case BoundBinaryOperatorKind.LogicalOr:
                    ILProcessor.Emit(OpCodes.Or);
                    return;
                case BoundBinaryOperatorKind.NumericAddition:
                    ILProcessor.Emit(OpCodes.Add);
                    return;
                case BoundBinaryOperatorKind.NumericSubstraction:
                    ILProcessor.Emit(OpCodes.Sub);
                    return;
                case BoundBinaryOperatorKind.StringConcatenation:
                    var methodReference = AssemblyDefinition.MainModule.ImportReference(StringConcatMethodInfo);
                    ILProcessor.Emit(OpCodes.Call, methodReference);
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
                    var parameterDefinition = ILProcessor.Body.Method.Parameters.FirstOrDefault(p => p.Name.Equals(parameter.Name));
                    ILProcessor.Emit(OpCodes.Ldarg, parameterDefinition);
                    break;
                case LocalVariableSymbol localVariable:
                    ILProcessor.Emit(OpCodes.Ldloca_S, variables[localVariable]);
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
            ILProcessor.Emit(OpCodes.Call, methodReference);
        }

    }
}
