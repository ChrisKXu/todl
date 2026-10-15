using System.Linq;
using FluentAssertions;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitLoopStatementTests
{
    [Fact]
    public void BoundLoopStatementsCanHaveEmptyBody()
    {
        TestUtils.EmitStatementAndVerify(
            "while true { }",
            TestInstruction.Create(OpCodes.Br_S, 3),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Brtrue_S, 2),
            TestInstruction.Create(OpCodes.Nop));

        TestUtils.EmitStatementAndVerify(
            "while false { }",
            TestInstruction.Create(OpCodes.Br_S, 3),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Ldc_I4_0),
            TestInstruction.Create(OpCodes.Brtrue_S, 2),
            TestInstruction.Create(OpCodes.Nop));

        TestUtils.EmitStatementAndVerify(
            "until true { }",
            TestInstruction.Create(OpCodes.Br_S, 3),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Brfalse_S, 2),
            TestInstruction.Create(OpCodes.Nop));

        TestUtils.EmitStatementAndVerify(
            "until false { }",
            TestInstruction.Create(OpCodes.Br_S, 3),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Ldc_I4_0),
            TestInstruction.Create(OpCodes.Brfalse_S, 2),
            TestInstruction.Create(OpCodes.Nop));
    }

    [Fact]
    public void BoundLoopStatementsCanHaveOneOrMoreInnerStatements()
    {
        TestUtils.EmitStatementAndVerify(
            "while Todl::Compiler::Tests::TestClass.PublicStaticIntField != 0 { System::Console.WriteLine(); }",
            TestInstruction.Create(OpCodes.Br_S, 8),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Call, "System.Void System.Console::WriteLine()"),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Ldsfld, "System.Int32 Todl.Compiler.Tests.TestClass::PublicStaticIntField"),
            TestInstruction.Create(OpCodes.Ldc_I4_0),
            TestInstruction.Create(OpCodes.Ceq),
            TestInstruction.Create(OpCodes.Ldc_I4_0),
            TestInstruction.Create(OpCodes.Ceq),
            TestInstruction.Create(OpCodes.Brtrue_S, 2),
            TestInstruction.Create(OpCodes.Nop));
    }

    [Fact]
    public void BreakStatementJumpsPastTheLoop()
    {
        TestUtils.EmitStatementAndVerify(
            "while true { break; }",
            TestInstruction.Create(OpCodes.Br_S, 5),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Br_S, 9),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Brtrue_S, 2),
            TestInstruction.Create(OpCodes.Nop));
    }

    [Fact]
    public void ContinueStatementJumpsToTheConditionRecheck()
    {
        TestUtils.EmitStatementAndVerify(
            "while true { continue; }",
            TestInstruction.Create(OpCodes.Br_S, 5),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Br_S, 5),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Brtrue_S, 2),
            TestInstruction.Create(OpCodes.Nop));
    }

    [Fact]
    public void LabeledBreakJumpsPastTheTargetOuterLoopNotTheInnerOne()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundStatement = TestUtils.BindStatement<BoundStatement>(
            "while true : outer { while true : inner { break outer; } }", diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();

        var emitter = new TestEmitter();
        emitter.EmitStatement(boundStatement);
        emitter.Emit();

        var instructions = emitter.ILProcessor.Body.Instructions;

        // The outer loop's break target is always the very last instruction emitted.
        var outerBreakTarget = instructions[^1];

        // Only `break outer;` should jump there, not the inner loop's own machinery.
        var jumpsToOuterExit = instructions.Where(i => ReferenceEquals(i.Operand, outerBreakTarget)).ToList();
        jumpsToOuterExit.Should().HaveCount(1);
        jumpsToOuterExit[0].OpCode.Should().Be(OpCodes.Br_S);
    }

    [Fact]
    public void LabeledContinueJumpsToTheTargetOuterLoopsConditionNotTheInnerOnes()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundStatement = TestUtils.BindStatement<BoundStatement>(
            "while true : outer { while true : inner { continue outer; } }", diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();

        var emitter = new TestEmitter();
        emitter.EmitStatement(boundStatement);
        emitter.Emit();

        var instructions = emitter.ILProcessor.Body.Instructions;

        // The outer loop's own opening jump targets its condition-recheck Nop.
        var outerConditionTarget = instructions[0].Operand;

        // Only that opening jump and `continue outer;` should target it.
        var jumpsToOuterCondition = instructions.Where(i => ReferenceEquals(i.Operand, outerConditionTarget)).ToList();
        jumpsToOuterCondition.Should().HaveCount(2);
        jumpsToOuterCondition.Should().Contain(instructions[0]);
    }

    [Fact]
    public void BreakAndContinueInsideRegionWithinLoopEmitLeave()
    {
        var emitter = new TestEmitter();
        var loop = BindLoop("while true { break; continue; }").As<BoundLoopStatement>();
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
        emitter.EmitStatement(BindLoop("while true { break; }"));
        emitter.ExitProtectedRegion();

        emitter.ILProcessor.Body.Instructions.Select(i => i.OpCode)
            .Should().NotContain(OpCodes.Leave);
    }

    private static BoundStatement BindLoop(string input)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundStatement = TestUtils.BindStatement<BoundStatement>(input, diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();
        return boundStatement;
    }
}
