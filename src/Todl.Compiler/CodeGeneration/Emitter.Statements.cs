using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding;

namespace Todl.Compiler.CodeGeneration;

internal sealed partial class Emitter
{
    private void EmitStatement(MethodBody methodBody, BoundStatement boundStatement)
    {
        switch (boundStatement)
        {
            case BoundReturnStatement boundReturnStatement:
                EmitReturnStatement(methodBody, boundReturnStatement);
                return;
            case BoundExpressionStatement boundExpressionStatement:
                EmitExpression(methodBody, boundExpressionStatement.Expression);
                return;
            default:
                return;
        }
    }

    private void EmitReturnStatement(MethodBody methodBody, BoundReturnStatement boundReturnStatement)
    {
        if (boundReturnStatement.BoundReturnValueExpression is not null)
        {
            EmitExpression(methodBody, boundReturnStatement.BoundReturnValueExpression);
        }

        methodBody.GetILProcessor().Emit(OpCodes.Ret);
    }
}
