using System;
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
        // Per-loop continue/break targets, keyed by BoundLoopContext so a labeled
        // break/continue can reach any enclosing loop (mirrors loopBlocks in the CFG).
        // Depth is the protected-region depth at which the targets live.
        private readonly Dictionary<BoundLoopContext, (Instruction ContinueTarget, Instruction BreakTarget, int Depth)> loopTargets = new();

        private int protectedRegionDepth;
        private Instruction returnEpilogue;
        private VariableDefinition returnLocal;

        // Both must bracket the emission of a protected region's contents so that jumps
        // leaving the region are emitted as `leave` rather than `br`.
        internal void EnterProtectedRegion() => protectedRegionDepth++;
        internal void ExitProtectedRegion() => protectedRegionDepth--;

        // Returns jump to the epilogue (which is outside every region) instead of emitting `ret`
        // inline. Without an epilogue (statement-only emission) `ret` is emitted directly.
        internal void ConfigureReturn(Instruction epilogue, VariableDefinition local)
        {
            returnEpilogue = epilogue;
            returnLocal = local;
        }

        internal void EmitReturnEpilogue()
        {
            ILProcessor.Append(returnEpilogue);
            if (returnLocal is not null)
            {
                EmitLocalLoad(returnLocal);
            }

            ILProcessor.Emit(OpCodes.Ret);
        }

        private void EmitJump(Instruction target, int targetDepth)
        {
            var opCode = protectedRegionDepth > targetDepth ? OpCodes.Leave : OpCodes.Br;
            ILProcessor.Emit(opCode, target);
        }

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
                case BoundNoOpStatement:
                    return;
                default:
                    throw new NotSupportedException($"Statement type {boundStatement.GetType().Name} is not supported.");
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

            if (returnEpilogue is null)
            {
                ILProcessor.Emit(OpCodes.Ret);
                return;
            }

            if (boundReturnStatement.BoundReturnValueExpression is not null)
            {
                EmitLocalStore(returnLocal);
            }

            EmitJump(returnEpilogue, 0);
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

            // Registered before the body so nested break/continue - including labeled - resolve.
            loopTargets[boundLoopStatement.BoundLoopContext] = (conditionLabel, breakLabel, protectedRegionDepth);

            ILProcessor.Emit(OpCodes.Br, conditionLabel);
            ILProcessor.Append(startLabel);
            EmitStatement(boundLoopStatement.Body);

            ILProcessor.Append(conditionLabel);
            EmitExpression(boundLoopStatement.Condition);

            // Long form: the loop body can easily exceed the short form's -128..127 byte
            // range, and Mono.Cecil does not auto-widen Brtrue_S/Brfalse_S, so using the
            // short form here silently truncates/corrupts the branch target once a loop
            // body grows past ~127 bytes.
            var opCode = boundLoopStatement.ConditionNegated
                ? OpCodes.Brfalse
                : OpCodes.Brtrue;

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

            var (_, breakTarget, depth) = loopTargets[boundBreakStatement.BoundLoopContext];
            EmitJump(breakTarget, depth);
        }

        private void EmitContinueStatement(BoundContinueStatement boundContinueStatement)
        {
            if (boundContinueStatement.BoundLoopContext is null)
            {
                // Already reported as NoEnclosingLoop/UndefinedLoopLabel during binding.
                return;
            }

            var (continueTarget, _, depth) = loopTargets[boundContinueStatement.BoundLoopContext];
            EmitJump(continueTarget, depth);
        }
    }
}
