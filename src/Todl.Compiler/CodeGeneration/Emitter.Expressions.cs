using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeGeneration;

internal partial class Emitter
{
    internal partial class InstructionEmitter
    {
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
                case BoundClrInvocationExpression boundClrInvocationExpression:
                    EmitClrInvocationExpression(boundClrInvocationExpression);
                    return;
                case BoundTodlInvocationExpression boundTodlInvocationExpression:
                    EmitTodlInvocationExpression(boundTodlInvocationExpression);
                    return;
                case BoundObjectCreationExpression boundObjectCreationExpression:
                    EmitObjectCreationExpression(boundObjectCreationExpression);
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

        private void EmitClrInvocationExpression(BoundClrInvocationExpression boundClrInvocationExpression)
        {
            // The "this" reference must be on the stack ahead of the arguments for an
            // instance call - pushing arguments first leaves an invalid evaluation stack.
            if (!boundClrInvocationExpression.IsStatic)
            {
                EmitInstanceCallReceiver(boundClrInvocationExpression.BoundBaseExpression);
            }

            foreach (var argument in boundClrInvocationExpression.BoundArguments)
            {
                EmitExpression(argument);
            }

            var methodReference = ResolveMethodReference(boundClrInvocationExpression);
            ILProcessor.Emit(OpCodes.Call, methodReference);
        }

        // A value-type receiver (local or parameter) needs its managed pointer on the stack,
        // not its value, so the callee can be invoked with `call` against the struct in place.
        private void EmitInstanceCallReceiver(BoundExpression baseExpression)
        {
            switch ((baseExpression as BoundVariableExpression)?.Variable)
            {
                case LocalVariableSymbol localVariableSymbol:
                    EmitLocalAddress(localVariableSymbol);
                    return;
                case ParameterSymbol parameterSymbol:
                    EmitParameterAddress(parameterSymbol);
                    return;
                default:
                    if (baseExpression.ResultType.IsReferenceType)
                    {
                        EmitExpression(baseExpression);
                        return;
                    }

                    // Not a local/parameter, so spill it to a temp local to make it addressable.
                    ILProcessor.Body.InitLocals = true;
                    var temp = new VariableDefinition(ResolveTypeReference(baseExpression.ResultType as ClrTypeSymbol));
                    ILProcessor.Body.Variables.Add(temp);
                    EmitExpression(baseExpression);
                    EmitLocalStore(temp);

                    if (temp.Index < 0xFF)
                    {
                        ILProcessor.Emit(OpCodes.Ldloca_S, temp);
                    }
                    else
                    {
                        ILProcessor.Emit(OpCodes.Ldloca, temp);
                    }
                    return;
            }
        }

        private void EmitObjectCreationExpression(BoundObjectCreationExpression boundObjectCreationExpression)
        {
            foreach (var argument in boundObjectCreationExpression.BoundArguments)
            {
                EmitExpression(argument);
            }

            var methodReference = ResolveMethodReference(boundObjectCreationExpression);
            ILProcessor.Emit(OpCodes.Newobj, methodReference);
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
                    // BoundBinaryOperatorKind.Comparison covers all four relational operators;
                    // the actual operator survives on Operator.SyntaxKind. `<=`/`>=` have no
                    // dedicated CIL opcode, so they're the negation of the strict opposite.
                    switch (boundBinaryExpression.Operator.SyntaxKind)
                    {
                        case SyntaxKind.LessThanToken:
                            ILProcessor.Emit(OpCodes.Clt);
                            return;
                        case SyntaxKind.GreaterThanToken:
                            ILProcessor.Emit(OpCodes.Cgt);
                            return;
                        case SyntaxKind.LessThanOrEqualsToken:
                            // left <= right ==> (left > right) == 0
                            ILProcessor.Emit(OpCodes.Cgt);
                            ILProcessor.Emit(OpCodes.Ldc_I4_0);
                            ILProcessor.Emit(OpCodes.Ceq);
                            return;
                        case SyntaxKind.GreaterThanOrEqualsToken:
                            // left >= right ==> (left < right) == 0
                            ILProcessor.Emit(OpCodes.Clt);
                            ILProcessor.Emit(OpCodes.Ldc_I4_0);
                            ILProcessor.Emit(OpCodes.Ceq);
                            return;
                        default:
                            throw new NotSupportedException($"{boundBinaryExpression.Operator.SyntaxKind} is not a supported comparison operator");
                    }
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
                case BoundBinaryOperatorKind.NumericMultiplication:
                    ILProcessor.Emit(OpCodes.Mul);
                    return;
                case BoundBinaryOperatorKind.NumericDivision:
                    // Only Int32 (signed) division is currently a supported binary operator, so
                    // Div (not Div_Un) matches every binder-resolved operand type today.
                    ILProcessor.Emit(OpCodes.Div);
                    return;
                default:
                    throw new NotSupportedException($"{boundBinaryExpression.Operator.BoundBinaryOperatorKind} is not a supported binary operator kind");
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
            var variableDefinition = Variables[localVariableSymbol];

            switch (variableDefinition.Index)
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
                    ILProcessor.Emit(OpCodes.Ldloc_S, variableDefinition);
                    return;
                default:
                    ILProcessor.Emit(OpCodes.Ldloc, variableDefinition);
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

            var variableDefinition = Variables[localVariableSymbol];
            if (variableDefinition.Index < 0xFF)
            {
                ILProcessor.Emit(OpCodes.Ldloca_S, variableDefinition);
            }
            else
            {
                ILProcessor.Emit(OpCodes.Ldloca, variableDefinition);
            }
        }

        private void EmitParameterAddress(ParameterSymbol parameterSymbol)
        {
            if (parameterSymbol.Type.IsReferenceType)
            {
                ILProcessor.Emit(OpCodes.Ldarg, Parameters[parameterSymbol]);
                return;
            }

            var parameterDefinition = Parameters[parameterSymbol];
            if (parameterDefinition.Index < 0xFF)
            {
                ILProcessor.Emit(OpCodes.Ldarga_S, parameterDefinition);
            }
            else
            {
                ILProcessor.Emit(OpCodes.Ldarga, parameterDefinition);
            }
        }

        private void EmitTodlInvocationExpression(BoundTodlInvocationExpression boundTodlInvocationExpression)
        {
            // BoundArguments is a name-keyed dictionary; its enumeration order is not guaranteed
            // to match declaration order, but positional argument pushes onto the stack must -
            // walk the function's declared parameter order instead of the dictionary's.
            foreach (var parameterName in boundTodlInvocationExpression.FunctionSymbol.OrderedParameterNames)
            {
                EmitExpression(boundTodlInvocationExpression.BoundArguments[parameterName]);
            }

            var methodReference = ResolveMethodReference(boundTodlInvocationExpression);
            ILProcessor.Emit(OpCodes.Call, methodReference);
        }

        private void EmitUnaryExpression(BoundUnaryExpression boundUnaryExpression, bool emitSideEffect)
        {
            var boundUnaryOperatorKind = boundUnaryExpression.Operator.BoundUnaryOperatorKind;
            EmitUnaryExpressionCore(boundUnaryExpression.Operand, boundUnaryOperatorKind);
        }

        private void EmitUnaryExpressionCore(BoundExpression operand, BoundUnaryOperatorKind boundUnaryOperatorKind)
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
            }
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
            var operatorKind = boundAssignmentExpression.Operator.BoundAssignmentOperatorKind;
            var isInline = operatorKind != BoundAssignmentExpression.BoundAssignmentOperatorKind.Assignment;

            EmitStore(boundAssignmentExpression.Left, () =>
            {
                // Inline operators (+=, -=, *=, /=) need the current value of the target
                // under the new one before applying the operator; plain `=` does not.
                if (isInline)
                {
                    EmitExpression(boundAssignmentExpression.Left);
                }

                EmitExpression(boundAssignmentExpression.Right);

                switch (operatorKind)
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
