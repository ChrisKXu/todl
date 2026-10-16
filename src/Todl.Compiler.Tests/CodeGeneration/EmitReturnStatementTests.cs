using System.Linq;
using FluentAssertions;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitReturnStatementTests
{
    [Fact]
    public void ReturnWithoutEpilogueEmitsInlineRet()
    {
        var emitter = new TestEmitter();
        emitter.EmitStatement(BindReturn("void F() { return; }"));

        emitter.ILProcessor.Body.Instructions.ShouldHaveExactInstructionSequence(
            TestInstruction.Create(OpCodes.Ret));
    }

    [Fact]
    public void ReturnJumpsToEpilogueWithBranchOutsideRegions()
    {
        var emitter = TestEmitter.CreateWithReturnEpilogue(out var epilogue);

        emitter.EmitStatement(BindReturn("int F() { return 1; }"));

        var instructions = emitter.ILProcessor.Body.Instructions;
        instructions.Select(i => i.OpCode).Should().Equal(OpCodes.Ldc_I4_1, OpCodes.Stloc_0, OpCodes.Br);
        instructions[^1].Operand.Should().BeSameAs(epilogue);
    }

    [Theory]
    [InlineData("int F() { try { return 1; } finally { } }")]
    [InlineData("int F() { try { } except { return 1; } }")]
    [InlineData("int F() { try { try { return 1; } finally { } } finally { } }")]
    public void ReturnInsideRegionEmitsLeave(string function)
    {
        var emitter = TestEmitter.CreateWithReturnEpilogue(out var epilogue);

        emitter.EmitStatement(TestUtils.BindMember<BoundFunctionMember>(function).Body);

        var instructions = emitter.ILProcessor.Body.Instructions;
        var returnJump = instructions.Single(i => ReferenceEquals(i.Operand, epilogue));
        returnJump.OpCode.Should().Be(OpCodes.Leave);
        instructions[instructions.IndexOf(returnJump) - 1].OpCode.Should().Be(OpCodes.Stloc_0);
    }

    private static BoundStatement BindReturn(string function)
        => TestUtils.BindMember<BoundFunctionMember>(function).Body.Statements[0];
}
