using System.Linq;
using FluentAssertions;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitTryStatementTests
{
    [Fact]
    public void ExceptClauseEmitsCatchHandlerWithoutVariable()
    {
        var emitter = Emit("try { } except System::FormatException { }");

        var instructions = emitter.ILProcessor.Body.Instructions;
        instructions.Select(i => i.OpCode).Should().Equal(
            OpCodes.Nop, OpCodes.Leave, OpCodes.Nop, OpCodes.Pop, OpCodes.Leave, OpCodes.Nop);

        var handler = emitter.ILProcessor.Body.ExceptionHandlers.Should().ContainSingle().Subject;
        handler.HandlerType.Should().Be(ExceptionHandlerType.Catch);
        handler.CatchType.FullName.Should().Be("System.FormatException");
        handler.TryStart.Should().BeSameAs(instructions[0]);
        handler.TryEnd.Should().BeSameAs(instructions[2]);
        handler.HandlerStart.Should().BeSameAs(instructions[2]);
        handler.HandlerEnd.Should().BeSameAs(instructions[^1]);
        instructions[1].Operand.Should().BeSameAs(instructions[^1]);
        instructions[4].Operand.Should().BeSameAs(instructions[^1]);
    }

    [Fact]
    public void ExceptClauseWithVariableStoresTheExceptionIntoALocal()
    {
        var emitter = Emit("try { } except System::FormatException e { e.Message; }");

        var body = emitter.ILProcessor.Body;
        body.Variables.Should().ContainSingle().Which.VariableType.FullName.Should().Be("System.FormatException");
        body.Instructions.Select(i => i.OpCode).Should().Contain(OpCodes.Stloc_0);
        body.Instructions.Select(i => i.OpCode).Should().NotContain(OpCodes.Pop);
    }

    [Fact]
    public void GeneralExceptClauseCatchesSystemObject()
    {
        var emitter = Emit("try { } except { }");

        emitter.ILProcessor.Body.ExceptionHandlers.Should().ContainSingle()
            .Which.CatchType.FullName.Should().Be("System.Object");
    }

    [Fact]
    public void ExceptClausesAreEmittedInSourceOrderOverTheSameTryRange()
    {
        var emitter = Emit("try { } except System::FormatException { } except System::Exception { } except { }");

        var handlers = emitter.ILProcessor.Body.ExceptionHandlers;
        handlers.Select(h => h.CatchType.FullName).Should().Equal("System.FormatException", "System.Exception", "System.Object");
        handlers.Select(h => h.TryStart).Distinct().Should().ContainSingle();
        handlers.Select(h => h.TryEnd).Distinct().Should().ContainSingle();
        handlers[0].TryEnd.Should().BeSameAs(handlers[0].HandlerStart);
        handlers[0].HandlerEnd.Should().BeSameAs(handlers[1].HandlerStart);
        handlers[1].HandlerEnd.Should().BeSameAs(handlers[2].HandlerStart);
    }

    [Fact]
    public void TryExceptFinallyNestsTheCatchRegionInsideTheFinallyRegion()
    {
        var emitter = Emit("try { } except { } finally { }");

        var instructions = emitter.ILProcessor.Body.Instructions;
        var handlers = emitter.ILProcessor.Body.ExceptionHandlers;
        handlers.Select(h => h.HandlerType).Should().Equal(ExceptionHandlerType.Catch, ExceptionHandlerType.Finally);

        var (catchHandler, finallyHandler) = (handlers[0], handlers[1]);
        finallyHandler.TryStart.Should().BeSameAs(instructions[0]);
        finallyHandler.TryEnd.Should().BeSameAs(finallyHandler.HandlerStart);
        finallyHandler.HandlerEnd.Should().BeSameAs(instructions[^1]);
        instructions.IndexOf(catchHandler.HandlerEnd).Should().BeLessThan(instructions.IndexOf(finallyHandler.HandlerStart));
        instructions[^2].OpCode.Should().Be(OpCodes.Endfinally);
    }

    [Fact]
    public void TryFinallyEmitsOnlyAFinallyHandler()
    {
        var emitter = Emit("try { } finally { }");

        emitter.ILProcessor.Body.ExceptionHandlers.Should().ContainSingle()
            .Which.HandlerType.Should().Be(ExceptionHandlerType.Finally);
    }

    [Fact]
    public void NestedTryStatementsPrecedeTheirEnclosingHandlersInTheTable()
    {
        var emitter = Emit("try { try { } except System::FormatException { } } except System::Exception { try { } finally { } }");

        var handlers = emitter.ILProcessor.Body.ExceptionHandlers;
        handlers.Select(h => (h.HandlerType, h.CatchType?.FullName)).Should().Equal(
            (ExceptionHandlerType.Catch, "System.FormatException"),
            (ExceptionHandlerType.Finally, null),
            (ExceptionHandlerType.Catch, "System.Exception"));
    }

    [Fact]
    public void ThrowEmitsTheExpressionThenThrow()
    {
        var emitter = Emit("throw new System::Exception();");

        emitter.ILProcessor.Body.Instructions.Select(i => i.OpCode).Should().Equal(OpCodes.Newobj, OpCodes.Throw);
    }

    [Fact]
    public void BareThrowEmitsRethrow()
    {
        var emitter = Emit("try { } except { throw; }");

        emitter.ILProcessor.Body.Instructions.Select(i => i.OpCode).Should().Contain(OpCodes.Rethrow);
    }

    private static TestEmitter Emit(string input)
    {
        var emitter = new TestEmitter();
        emitter.EmitStatement(TestUtils.BindStatement<BoundStatement>(input));
        return emitter;
    }
}
