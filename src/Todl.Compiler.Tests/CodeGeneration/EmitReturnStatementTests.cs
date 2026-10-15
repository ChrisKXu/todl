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

    [Fact]
    public void ReturnInsideRegionEmitsLeave()
    {
        var emitter = TestEmitter.CreateWithReturnEpilogue(out var epilogue);

        emitter.EnterProtectedRegion();
        emitter.EmitStatement(BindReturn("int F() { return 1; }"));
        emitter.ExitProtectedRegion();

        var instructions = emitter.ILProcessor.Body.Instructions;
        instructions.Select(i => i.OpCode).Should().Equal(OpCodes.Ldc_I4_1, OpCodes.Stloc_0, OpCodes.Leave);
        instructions[^1].Operand.Should().BeSameAs(epilogue);
    }

    private static BoundStatement BindReturn(string function)
        => TestUtils.BindMember<BoundFunctionMember>(function).Body.Statements[0];
}
