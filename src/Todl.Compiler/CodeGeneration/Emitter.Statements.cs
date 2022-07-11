using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding;

namespace Todl.Compiler.CodeGeneration;

internal sealed partial class Emitter
{
    private void EmitStatement(MethodBody methodBody, BoundStatement boundStatement)
    {
        switch (boundStatement)
        {
            case BoundBlockStatement boundBlockStatement:
                EmitBlockStatement(methodBody, boundBlockStatement);
                return;
            case BoundReturnStatement boundReturnStatement:
                EmitReturnStatement(methodBody, boundReturnStatement);
                return;
            case BoundExpressionStatement boundExpressionStatement:
                EmitExpression(methodBody, boundExpressionStatement.Expression);
                return;
            case BoundConditionalStatement boundConditionalStatement:
                EmitConditionalStatement(methodBody, boundConditionalStatement);
                return;
            default:
                return;
        }
    }

    private void EmitBlockStatement(MethodBody methodBody, BoundBlockStatement boundBlockStatement)
    {
        foreach(var statement in boundBlockStatement.Statements)
        {
            EmitStatement(methodBody, statement);
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

    private void EmitConditionalStatement(MethodBody methodBody, BoundConditionalStatement boundConditionalStatement)
    {
        EmitExpression(methodBody, boundConditionalStatement.Condition);

        var ilProcessor = methodBody.GetILProcessor();

        var elseLabel = ilProcessor.Create(OpCodes.Nop);
        ilProcessor.Emit(OpCodes.Brfalse, elseLabel);

        EmitStatement(methodBody, boundConditionalStatement.Consequence);

        ilProcessor.Append(elseLabel);

        EmitStatement(methodBody, boundConditionalStatement.Alternative);
    }
}
