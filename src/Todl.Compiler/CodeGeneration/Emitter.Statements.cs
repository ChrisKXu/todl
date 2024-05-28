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
                    EmitExpressionStatement(boundExpressionStatement);
                    return;
                case BoundConditionalStatement boundConditionalStatement:
                    EmitConditionalStatement(boundConditionalStatement);
                    return;
                case BoundVariableDeclarationStatement boundVariableDeclarationStatement:
                    EmitVariableDeclarationStatement(boundVariableDeclarationStatement);
                    return;
                case BoundLoopStatement boundLoopStatement:
                    EmitLoopStatement(boundLoopStatement);
                    return;
                default:
                    return;
            }
        }

        private void EmitExpressionStatement(BoundExpressionStatement boundExpressionStatement)
        {
            // standalone a++ shouldn't trigger side effect
            if (boundExpressionStatement.Expression is BoundUnaryExpression boundUnaryExpression)
            {
                EmitUnaryExpression(boundUnaryExpression, false);
                return;
            }

            EmitExpression(boundExpressionStatement.Expression);
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
            Variables[variable] = variableDefinition;

            if (boundVariableDeclarationStatement.InitializerExpression is not null)
            {
                EmitExpression(boundVariableDeclarationStatement.InitializerExpression);
                EmitLocalStore(variableDefinition);
            }
        }

        private void EmitLoopStatement(BoundLoopStatement boundLoopStatement)
        {
            var startLabel = ILProcessor.Create(OpCodes.Nop);
            var conditionLabel = ILProcessor.Create(OpCodes.Nop);

            ILProcessor.Emit(OpCodes.Br, conditionLabel);
            ILProcessor.Append(startLabel);
            EmitStatement(boundLoopStatement.Body);

            ILProcessor.Append(conditionLabel);
            EmitExpression(boundLoopStatement.Condition);

            var opCode = boundLoopStatement.ConditionNegated
                ? OpCodes.Brfalse_S
                : OpCodes.Brtrue_S;

            ILProcessor.Emit(opCode, startLabel);
        }
    }
}
