using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
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
                case BoundAssignmentExpression boundAssignmentExpression:
                    EmitAssignmentExpression(boundAssignmentExpression);
                    return;
                case BoundClrFunctionCallExpression boundClrFunctionCallExpression:
                    EmitClrFunctionCallExpression(boundClrFunctionCallExpression);
                    return;
                case BoundTodlFunctionCallExpression boundTodlFunctionCallExpression:
                    EmitTodlFunctionCallExpression(boundTodlFunctionCallExpression);
                    return;
                case BoundUnaryExpression boundUnaryExpression:
                    EmitUnaryExpression(boundUnaryExpression, true);
                    return;
                case BoundBinaryExpression boundBinaryExpression:
                    EmitBinaryExpression(boundBinaryExpression);
                    return;
                case BoundVariableExpression boundVariableExpression:
                    EmitVariableExpression(boundVariableExpression);
                    return;
                case BoundMemberAccessExpression boundMemberAccessExpression:
                    EmitMemberAccessExpression(boundMemberAccessExpression);
                    return;
                default:
                    throw new NotSupportedException($"Expression type {boundExpression.GetType().Name} is not supported.");
            }
        }

        private void EmitConstant(BoundConstant boundConstant)
        {
            switch (boundConstant.Value)
            {
                case ConstantNullValue:
                    ILProcessor.Emit(OpCodes.Ldnull);
                    return;
                case ConstantStringValue constantStringValue:
                    ILProcessor.Emit(OpCodes.Ldstr, constantStringValue.StringValue);
                    return;
                case ConstantBooleanValue constantBooleanValue:
                    EmitIntValue(constantBooleanValue.Int32Value);
                    return;
                case ConstantFloatValue constantFloatValue:
                    EmitFloatValue(constantFloatValue.FloatValue);
                    return;
                case ConstantDoubleValue constantDoubleValue:
                    EmitDoubleValue(constantDoubleValue.DoubleValue);
                    return;
                case ConstantInt32Value:
                case ConstantUInt32Value:
                    EmitIntValue(boundConstant.Value.Int32Value);
                    return;
                case ConstantInt64Value:
                case ConstantUInt64Value:
                    EmitInt64Value(boundConstant.Value.Int64Value);
                    return;
            }
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

        private void EmitInt64Value(long longValue)
        {
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

        private void EmitFloatValue(float floatValue)
        {
            ILProcessor.Emit(OpCodes.Ldc_R4, floatValue);
        }

        private void EmitDoubleValue(double doubleValue)
        {
            ILProcessor.Emit(OpCodes.Ldc_R8, doubleValue);
        }

        private void EmitClrFunctionCallExpression(BoundClrFunctionCallExpression boundClrFunctionCallExpression)
        {
            foreach (var argument in boundClrFunctionCallExpression.BoundArguments)
            {
                EmitExpression(argument);
            }

            if (!boundClrFunctionCallExpression.IsStatic)
            {
                if (boundClrFunctionCallExpression.BoundBaseExpression is BoundVariableExpression boundVariableExpression
                    && boundVariableExpression.Variable is LocalVariableSymbol localVariableSymbol)
                {
                    EmitLocalAddress(localVariableSymbol);
                }
                else
                {
                    EmitExpression(boundClrFunctionCallExpression.BoundBaseExpression);
                }
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
                case BoundBinaryOperatorKind.Inequality:
                    // left != right ==> (left == right) == 0
                    ILProcessor.Emit(OpCodes.Ceq);
                    ILProcessor.Emit(OpCodes.Ldc_I4_0);
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
                    EmitLocalLoad(localVariable);
                    break;
                default:
                    throw new NotSupportedException($"{boundVariableExpression.Variable} is not supported");
            }
        }

        private void EmitLocalLoad(LocalVariableSymbol localVariableSymbol)
        {
            var slot = Variables[localVariableSymbol].Index;

            switch (slot)
            {
                case 0:
                    ILProcessor.Emit(OpCodes.Ldloc_0);
                    return;
                case 1:
                    ILProcessor.Emit(OpCodes.Ldloc_1);
                    return;
                case 2:
                    ILProcessor.Emit(OpCodes.Ldloc_2);
                    return;
                case 3:
                    ILProcessor.Emit(OpCodes.Ldloc_3);
                    return;
                case < 0xFF:
                    ILProcessor.Emit(OpCodes.Ldloc_S, (sbyte)slot);
                    return;
                default:
                    ILProcessor.Emit(OpCodes.Ldloc, slot);
                    return;
            }
        }

        private void EmitLocalAddress(LocalVariableSymbol localVariableSymbol)
        {
            if (localVariableSymbol.Type.IsReferenceType)
            {
                EmitLocalLoad(localVariableSymbol);
                return;
            }

            var slot = Variables[localVariableSymbol].Index;
            if (slot < 0xFF)
            {
                ILProcessor.Emit(OpCodes.Ldloca_S, (byte)slot);
            }
            else
            {
                ILProcessor.Emit(OpCodes.Ldloca, slot);
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

        private void EmitUnaryExpression(BoundUnaryExpression boundUnaryExpression, bool emitSideEffect)
        {
            var boundUnaryOperatorKind = boundUnaryExpression.Operator.BoundUnaryOperatorKind;

            if (!boundUnaryOperatorKind.HasSideEffect())
            {
                EmitUnaryExpressionWithoutSideEffect(boundUnaryExpression.Operand, boundUnaryOperatorKind);
            }
            else
            {
                EmitStore(boundUnaryExpression.Operand, () =>
                {
                    EmitUnaryExpressionWithoutSideEffect(boundUnaryExpression.Operand, boundUnaryOperatorKind);
                    EmitUnaryOperatorWithSideEffect(boundUnaryOperatorKind, emitSideEffect);
                });
            }
        }

        private void EmitUnaryExpressionWithoutSideEffect(BoundExpression operand, BoundUnaryOperatorKind boundUnaryOperatorKind)
        {
            EmitExpression(operand);

            switch (boundUnaryOperatorKind.GetOperationKind())
            {
                case BoundUnaryOperatorKind.UnaryMinus:
                    if (boundUnaryOperatorKind.GetOperandKind() == BoundUnaryOperatorKind.UInt)
                    {
                        ILProcessor.Emit(OpCodes.Conv_U8);
                    }
                    ILProcessor.Emit(OpCodes.Neg);
                    return;
                case BoundUnaryOperatorKind.BitwiseComplement:
                    ILProcessor.Emit(OpCodes.Not);
                    return;
                case BoundUnaryOperatorKind.LogicalNegation:
                    // !a is emitted as (a == 0)
                    ILProcessor.Emit(OpCodes.Ldc_I4_0);
                    ILProcessor.Emit(OpCodes.Ceq);
                    return;
                default:
                    break;
            }
        }

        private void EmitUnaryOperatorWithSideEffect(BoundUnaryOperatorKind boundUnaryOperatorKind, bool emitSideEffect)
        {
            var operationKind = boundUnaryOperatorKind.GetOperationKind();

            var opCode =
                operationKind == BoundUnaryOperatorKind.PrefixIncrement
                || operationKind == BoundUnaryOperatorKind.PostfixIncrement
                ? OpCodes.Add
                : OpCodes.Sub;

            var prefix = operationKind == BoundUnaryOperatorKind.PrefixIncrement || operationKind == BoundUnaryOperatorKind.PrefixDecrement;

            if (!prefix && emitSideEffect)
            {
                ILProcessor.Emit(OpCodes.Dup);
            }

            switch (boundUnaryOperatorKind.GetOperandKind())
            {
                case BoundUnaryOperatorKind.Long:
                case BoundUnaryOperatorKind.ULong:
                    EmitInt64Value(1L);
                    break;
                case BoundUnaryOperatorKind.Float:
                    EmitFloatValue(1.0F);
                    break;
                case BoundUnaryOperatorKind.Double:
                    EmitDoubleValue(1.0);
                    break;
                default:
                    EmitIntValue(1);
                    break;
            }

            ILProcessor.Emit(opCode);

            if (prefix && emitSideEffect)
            {
                ILProcessor.Emit(OpCodes.Dup);
            }
        }

        private void EmitMemberAccessExpression(BoundMemberAccessExpression boundMemberAccessExpression)
        {
            if (!boundMemberAccessExpression.IsStatic)
            {
                EmitExpression(boundMemberAccessExpression.BoundBaseExpression);
            }

            switch (boundMemberAccessExpression)
            {
                case BoundClrFieldAccessExpression boundClrFieldAccessExpression:
                    EmitClrFieldLoad(boundClrFieldAccessExpression);
                    return;
                case BoundClrPropertyAccessExpression boundClrPropertyAccessExpression:
                    EmitClrPropertyLoad(boundClrPropertyAccessExpression);
                    return;
            }
        }

        private void EmitClrFieldLoad(BoundClrFieldAccessExpression boundClrFieldAccessExpression)
        {
            var baseType = ResolveTypeReference(boundClrFieldAccessExpression.ResultType as ClrTypeSymbol);
            var opCode = boundClrFieldAccessExpression.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld;
            ILProcessor.Emit(opCode, new FieldReference(boundClrFieldAccessExpression.MemberName, baseType));
        }

        private void EmitClrFieldStore(BoundClrFieldAccessExpression boundClrFieldAccessExpression)
        {
            var baseType = ResolveTypeReference(boundClrFieldAccessExpression.ResultType as ClrTypeSymbol);
            var opCode = boundClrFieldAccessExpression.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld;
            ILProcessor.Emit(opCode, new FieldReference(boundClrFieldAccessExpression.MemberName, baseType));
        }

        private void EmitClrPropertyLoad(BoundClrPropertyAccessExpression boundClrPropertyAccessExpression)
        {
            var methodReference = AssemblyDefinition.MainModule.ImportReference(boundClrPropertyAccessExpression.GetMethod);
            var opCode = boundClrPropertyAccessExpression.IsStatic ? OpCodes.Call : OpCodes.Callvirt;
            ILProcessor.Emit(opCode, methodReference);
        }

        private void EmitClrPropertyStore(BoundClrPropertyAccessExpression boundClrPropertyAccessExpression)
        {
            var methodReference = AssemblyDefinition.MainModule.ImportReference(boundClrPropertyAccessExpression.SetMethod);
            var opCode = boundClrPropertyAccessExpression.IsStatic ? OpCodes.Call : OpCodes.Callvirt;
            ILProcessor.Emit(opCode, methodReference);
        }

        private void EmitStore(BoundExpression left, Action assignmentAction)
        {
            if (left is BoundMemberAccessExpression boundMemberAccessExpression
                && !boundMemberAccessExpression.IsStatic)
            {
                EmitExpression(boundMemberAccessExpression.BoundBaseExpression);
            }

            assignmentAction();

            switch (left)
            {
                case BoundVariableExpression boundVariableExpression:
                    switch (boundVariableExpression.Variable)
                    {
                        case LocalVariableSymbol localVariableSymbol:
                            EmitLocalStore(Variables[localVariableSymbol]);
                            break;
                        case ParameterSymbol parameterSymbol:
                            EmitArgStore(Parameters[parameterSymbol]);
                            break;
                        default:
                            throw new NotSupportedException($"{boundVariableExpression.Variable} is not supported");
                    }
                    break;
                case BoundClrFieldAccessExpression boundClrFieldAccessExpression:
                    EmitClrFieldStore(boundClrFieldAccessExpression);
                    break;
                case BoundClrPropertyAccessExpression boundClrPropertyAccessExpression:
                    EmitClrPropertyStore(boundClrPropertyAccessExpression);
                    break;
            }
        }

        // Logic from https://github.com/dotnet/roslyn/blob/80b5e0207776a6dc911def62a6f7bcc3d3f7b33b/src/Compilers/Core/Portable/CodeGen/ILBuilderEmit.cs
        private void EmitLocalStore(VariableDefinition variableDefinition)
        {
            switch (variableDefinition.Index)
            {
                case 0:
                    ILProcessor.Emit(OpCodes.Stloc_0);
                    return;
                case 1:
                    ILProcessor.Emit(OpCodes.Stloc_1);
                    return;
                case 2:
                    ILProcessor.Emit(OpCodes.Stloc_2);
                    return;
                case 3:
                    ILProcessor.Emit(OpCodes.Stloc_3);
                    return;
                case < 0xFF:
                    ILProcessor.Emit(OpCodes.Stloc_S, variableDefinition);
                    return;
                default:
                    ILProcessor.Emit(OpCodes.Stloc, variableDefinition);
                    return;
            };
        }

        private void EmitArgStore(ParameterDefinition parameterDefinition)
        {
            if (parameterDefinition.Index < 0xFF)
            {
                ILProcessor.Emit(OpCodes.Starg_S, parameterDefinition);
            }
            else
            {
                ILProcessor.Emit(OpCodes.Starg, parameterDefinition);
            }
        }

        private void EmitAssignmentExpression(BoundAssignmentExpression boundAssignmentExpression)
        {
            EmitStore(boundAssignmentExpression.Left, () =>
            {
                EmitExpression(boundAssignmentExpression.Right);

                switch (boundAssignmentExpression.Operator.BoundAssignmentOperatorKind)
                {
                    case BoundAssignmentExpression.BoundAssignmentOperatorKind.AdditionInline:
                        ILProcessor.Emit(OpCodes.Add);
                        break;
                    case BoundAssignmentExpression.BoundAssignmentOperatorKind.SubstractionInline:
                        ILProcessor.Emit(OpCodes.Sub);
                        break;
                    case BoundAssignmentExpression.BoundAssignmentOperatorKind.MultiplicationInline:
                        ILProcessor.Emit(OpCodes.Mul);
                        break;
                    case BoundAssignmentExpression.BoundAssignmentOperatorKind.DivisionInline:
                        ILProcessor.Emit(OpCodes.Div);
                        break;
                }
            });
        }
    }
}
