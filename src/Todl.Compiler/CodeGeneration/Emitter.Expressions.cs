using System;
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
            default:
                throw new NotSupportedException();
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

        if (methodReference.ReturnType != assemblyDefinition.MainModule.TypeSystem.Void)
        {
            methodBody.GetILProcessor().Emit(OpCodes.Pop);
        }
    }
}
