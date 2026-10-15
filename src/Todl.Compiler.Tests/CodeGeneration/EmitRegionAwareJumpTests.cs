using System.Linq;
using FluentAssertions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitRegionAwareJumpTests
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
        var emitter = CreateEmitterWithEpilogue(out var epilogue);

        emitter.EmitStatement(BindReturn("int F() { return 1; }"));

        var instructions = emitter.ILProcessor.Body.Instructions;
        instructions.Select(i => i.OpCode).Should().Equal(OpCodes.Ldc_I4_1, OpCodes.Stloc_0, OpCodes.Br);
        instructions[^1].Operand.Should().BeSameAs(epilogue);
    }

    [Fact]
    public void ReturnInsideRegionEmitsLeave()
    {
        var emitter = CreateEmitterWithEpilogue(out var epilogue);

        emitter.EnterProtectedRegion();
        emitter.EmitStatement(BindReturn("int F() { return 1; }"));
        emitter.ExitProtectedRegion();

        var instructions = emitter.ILProcessor.Body.Instructions;
        instructions.Select(i => i.OpCode).Should().Equal(OpCodes.Ldc_I4_1, OpCodes.Stloc_0, OpCodes.Leave);
        instructions[^1].Operand.Should().BeSameAs(epilogue);
    }

    [Fact]
    public void BreakAndContinueInsideRegionWithinLoopEmitLeave()
    {
        var emitter = new TestEmitter();
        var loop = Bind("while true { break; continue; }").As<BoundLoopStatement>();
        var body = loop.Body.As<BoundBlockStatement>();

        // Emit the loop normally, then re-emit the body statements inside a region:
        // the loop's targets stay registered at depth 0.
        emitter.EmitStatement(loop);
        var before = emitter.ILProcessor.Body.Instructions.Count;

        emitter.EnterProtectedRegion();
        foreach (var statement in body.Statements)
        {
            emitter.EmitStatement(statement);
        }

        emitter.ExitProtectedRegion();

        emitter.ILProcessor.Body.Instructions.Skip(before).Select(i => i.OpCode)
            .Should().Equal(OpCodes.Leave, OpCodes.Leave);
    }

    [Fact]
    public void LoopEntirelyInsideRegionJumpsWithBr()
    {
        var emitter = new TestEmitter();

        // A loop that is itself inside a region jumps within that region.
        emitter.EnterProtectedRegion();
        emitter.EmitStatement(Bind("while true { break; }"));
        emitter.ExitProtectedRegion();

        emitter.ILProcessor.Body.Instructions.Select(i => i.OpCode)
            .Should().NotContain(OpCodes.Leave);
    }

    private static TestEmitter CreateEmitterWithEpilogue(out Instruction epilogue)
    {
        var emitter = new TestEmitter();
        epilogue = emitter.ILProcessor.Create(OpCodes.Nop);

        var returnLocal = new VariableDefinition(emitter.AssemblyDefinition.MainModule.TypeSystem.Int32);
        emitter.ILProcessor.Body.Variables.Add(returnLocal);
        emitter.ConfigureReturn(epilogue, returnLocal);
        return emitter;
    }

    private static BoundStatement BindReturn(string function)
        => TestUtils.BindMember<BoundFunctionMember>(function).Body.Statements[0];

    private static BoundStatement Bind(string input)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundStatement = TestUtils.BindStatement<BoundStatement>(input, diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();
        return boundStatement;
    }
}
