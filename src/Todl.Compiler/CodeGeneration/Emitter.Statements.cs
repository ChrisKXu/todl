using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeGeneration;

internal partial class Emitter
{
    internal partial class InstructionEmitter
    {
        public void EmitStatement(BoundStatement boundStatement)
        {
            switch (boundStatement)
            {
                case BoundBlockStatement boundBlockStatement:
                    EmitBlockStatement(boundBlockStatement);
                    return;
                case BoundReturnStatement boundReturnStatement:
                    EmitReturnStatement(boundReturnStatement);
                    return;
                case BoundExpressionStatement boundExpressionStatement:
                    EmitExpression(boundExpressionStatement.Expression);
                    return;
                case BoundConditionalStatement boundConditionalStatement:
                    EmitConditionalStatement(boundConditionalStatement);
                    return;
                case BoundVariableDeclarationStatement boundVariableDeclarationStatement:
                    EmitVariableDeclarationStatement(boundVariableDeclarationStatement);
                    return;
                default:
                    return;
            }
        }

        private void EmitBlockStatement(BoundBlockStatement boundBlockStatement)
        {
            foreach (var statement in boundBlockStatement.Statements)
            {
                EmitStatement(statement);
            }
        }

        private void EmitReturnStatement(BoundReturnStatement boundReturnStatement)
        {
            if (boundReturnStatement.BoundReturnValueExpression is not null)
            {
                EmitExpression(boundReturnStatement.BoundReturnValueExpression);
            }

            ILProcessor.Emit(OpCodes.Ret);
        }

        private void EmitConditionalStatement(BoundConditionalStatement boundConditionalStatement)
        {
            EmitExpression(boundConditionalStatement.Condition);

            var elseLabel = ILProcessor.Create(OpCodes.Nop);
            var continuationLabel = ILProcessor.Create(OpCodes.Nop);

            ILProcessor.Emit(OpCodes.Brfalse, elseLabel);

            EmitStatement(boundConditionalStatement.Consequence);
            ILProcessor.Emit(OpCodes.Br, continuationLabel);

            ILProcessor.Append(elseLabel);

            EmitStatement(boundConditionalStatement.Alternative);
            ILProcessor.Emit(OpCodes.Br, continuationLabel);

            ILProcessor.Append(continuationLabel);
        }

        private void EmitVariableDeclarationStatement(BoundVariableDeclarationStatement boundVariableDeclarationStatement)
        {
            ILProcessor.Body.InitLocals = true;

            var variable = boundVariableDeclarationStatement.Variable;
            var variableDefinition = new VariableDefinition(ResolveTypeReference(variable.Type as ClrTypeSymbol));
            ILProcessor.Body.Variables.Add(variableDefinition);
            variables[variable] = variableDefinition;

            if (boundVariableDeclarationStatement.InitializerExpression is not null)
            {
                EmitExpression(boundVariableDeclarationStatement.InitializerExpression, true);
                EmitLocalStore(variableDefinition);
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
    }
}
