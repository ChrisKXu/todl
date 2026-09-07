using System.Collections.Generic;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeGeneration;

internal partial class Emitter
{
    internal partial class InstructionEmitter
    {
        // Tracks each loop's continue target (condition re-check) and break target
        // (first instruction after the loop), keyed by the loop's own BoundLoopContext so
        // a labeled break/continue can jump directly to any enclosing loop, not just the
        // innermost one - mirroring ControlFlowGraph.Builder's loopBlocks.
        private readonly Dictionary<BoundLoopContext, (Instruction ContinueTarget, Instruction BreakTarget)> loopTargets = new();

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
                case BoundBreakStatement boundBreakStatement:
                    EmitBreakStatement(boundBreakStatement);
                    return;
                case BoundContinueStatement boundContinueStatement:
                    EmitContinueStatement(boundContinueStatement);
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
            var breakLabel = ILProcessor.Create(OpCodes.Nop);

            // Registered before emitting the body so nested break/continue statements -
            // including labeled ones targeting this exact loop - resolve correctly.
            loopTargets[boundLoopStatement.BoundLoopContext] = (conditionLabel, breakLabel);

            ILProcessor.Emit(OpCodes.Br, conditionLabel);
            ILProcessor.Append(startLabel);
            EmitStatement(boundLoopStatement.Body);

            ILProcessor.Append(conditionLabel);
            EmitExpression(boundLoopStatement.Condition);

            var opCode = boundLoopStatement.ConditionNegated
                ? OpCodes.Brfalse_S
                : OpCodes.Brtrue_S;

            ILProcessor.Emit(opCode, startLabel);
            ILProcessor.Append(breakLabel);
        }

        private void EmitBreakStatement(BoundBreakStatement boundBreakStatement)
        {
            if (boundBreakStatement.BoundLoopContext is null)
            {
                // Already reported as NoEnclosingLoop/UndefinedLoopLabel during binding.
                return;
            }

            var (_, breakTarget) = loopTargets[boundBreakStatement.BoundLoopContext];
            ILProcessor.Emit(OpCodes.Br, breakTarget);
        }

        private void EmitContinueStatement(BoundContinueStatement boundContinueStatement)
        {
            if (boundContinueStatement.BoundLoopContext is null)
            {
                // Already reported as NoEnclosingLoop/UndefinedLoopLabel during binding.
                return;
            }

            var (continueTarget, _) = loopTargets[boundContinueStatement.BoundLoopContext];
            ILProcessor.Emit(OpCodes.Br, continueTarget);
        }
    }
}
