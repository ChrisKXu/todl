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
            TestInstruction.Create(OpCodes.Ldsfld, "System.Int32 PublicStaticIntField"),
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

        // The outer loop's break-target Nop is always the very last instruction emitted.
        var outerBreakTarget = instructions[^1];
        outerBreakTarget.OpCode.Should().Be(OpCodes.Nop);

        // Only `break outer;` should jump there - not the inner loop's own machinery, which
        // would instead target the inner loop's own (earlier) break/condition labels.
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

        // The outer loop's own opening instruction always jumps to its condition-recheck -
        // that target Nop is what a correctly-resolved `continue outer;` must also jump to.
        var outerConditionTarget = instructions[0].Operand;

        // Exactly two instructions should target it: the outer loop's own opening jump, and
        // `continue outer;` from inside the inner loop. Nothing belonging to the inner loop
        // should ever point here.
        var jumpsToOuterCondition = instructions.Where(i => ReferenceEquals(i.Operand, outerConditionTarget)).ToList();
        jumpsToOuterCondition.Should().HaveCount(2);
        jumpsToOuterCondition.Should().Contain(instructions[0]);
    }
}
