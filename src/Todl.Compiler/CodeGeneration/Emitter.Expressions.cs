using System;
using System.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeGeneration;

internal sealed partial class Emitter
{
    private void EmitExpression(MethodBody methodBody, BoundExpression boundExpression)
    {
        switch (boundExpression)
        {
            case BoundConstant boundConstant:
                EmitConstant(methodBody, boundConstant);
                return;
            case BoundClrFunctionCallExpression boundClrFunctionCallExpression:
                EmitClrFunctionCallExpression(methodBody, boundClrFunctionCallExpression);
                return;
            case BoundTodlFunctionCallExpression boundTodlFunctionCallExpression:
                EmitTodlFunctionCallExpression(methodBody, boundTodlFunctionCallExpression);
                return;
            case BoundBinaryExpression boundBinaryExpression:
                EmitBinaryExpression(methodBody, boundBinaryExpression);
                return;
            case BoundVariableExpression boundVariableExpression:
                EmitVariableExpression(methodBody, boundVariableExpression);
                return;
            default:
                throw new NotSupportedException($"Expression type {boundExpression.GetType().Name} is not supported.");
        }
    }

    private void EmitConstant(MethodBody methodBody, BoundConstant boundConstant)
    {
        if (boundConstant.ResultType.Equals(BuiltInTypes.Int32))
        {
            methodBody.GetILProcessor().Emit(OpCodes.Ldc_I4, (int)boundConstant.Value);
        }
        else if (boundConstant.ResultType.Equals(BuiltInTypes.String))
        {
            methodBody.GetILProcessor().Emit(OpCodes.Ldstr, (string)boundConstant.Value);
        }
    }

    private void EmitClrFunctionCallExpression(MethodBody methodBody, BoundClrFunctionCallExpression boundClrFunctionCallExpression)
    {
        foreach (var argument in boundClrFunctionCallExpression.BoundArguments)
        {
            EmitExpression(methodBody, argument);
        }

        var methodReference = ResolveMethodReference(boundClrFunctionCallExpression);
        methodBody.GetILProcessor().Emit(OpCodes.Call, methodReference);
    }

    private void EmitBinaryExpression(MethodBody methodBody, BoundBinaryExpression boundBinaryExpression)
    {
        EmitExpression(methodBody, boundBinaryExpression.Left);
        EmitExpression(methodBody, boundBinaryExpression.Right);

        switch (boundBinaryExpression.Operator.BoundBinaryOperatorKind)
        {
            case BoundBinaryOperatorKind.Equality:
                methodBody.GetILProcessor().Emit(OpCodes.Ceq);
                return;
            case BoundBinaryOperatorKind.Comparison:
                methodBody.GetILProcessor().Emit(OpCodes.Cgt);
                return;
            case BoundBinaryOperatorKind.LogicalAnd:
            case BoundBinaryOperatorKind.LogicalOr:
                return;
            default:
                return;
        }
    }

    private void EmitVariableExpression(MethodBody methodBody, BoundVariableExpression boundVariableExpression)
    {
        if (boundVariableExpression.Variable is ParameterSymbol)
        {
            methodBody.GetILProcessor().Emit(OpCodes.Ldarg_0);
        }
    }

    private void EmitTodlFunctionCallExpression(MethodBody methodBody, BoundTodlFunctionCallExpression boundTodlFunctionCallExpression)
    {
        Debug.Assert(methodReferences.ContainsKey(boundTodlFunctionCallExpression.FunctionSymbol));

        foreach (var argument in boundTodlFunctionCallExpression.BoundArguments.Values)
        {
            EmitExpression(methodBody, argument);
        }

        var methodReference = methodReferences[boundTodlFunctionCallExpression.FunctionSymbol];
        methodBody.GetILProcessor().Emit(OpCodes.Call, methodReference);
    }
}
