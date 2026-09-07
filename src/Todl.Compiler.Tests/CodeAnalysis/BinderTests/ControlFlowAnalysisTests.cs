using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Binding.ControlFlowAnalysis;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class ControlFlowAnalysisTests
{
    [Theory]
    [InlineData("void func() { }")]
    [InlineData("void func() { int.MaxValue.ToString(); }")]
    [InlineData("void func() { return; }")]
    [InlineData("void func() { int.MaxValue.ToString(); return; }")]
    [InlineData("void func() { if true { int.MaxValue.ToString(); } }")]
    [InlineData("void func() { if true { return; } }")]
    [InlineData("void func() { if true { return; } int.MaxValue.ToString(); }")]
    [InlineData("int func() { return int.MaxValue; }")]
    [InlineData("int func() { int.MaxValue.ToString(); return int.MaxValue; }")]
    [InlineData("int func() { if true { return int.MaxValue; } return 0; }")]
    [InlineData("int func() { if true { return int.MaxValue; } else { return 0; } }")]
    [InlineData("int func() { if true { } return 0; }")]
    [InlineData("int func() { const a = 3; if a == 0 { return int.MaxValue; } else if a == 1 { return 1; } else { return 0; } }")]
    [InlineData("int func() { const a = 3; if a == 0 { return int.MaxValue; } else { if a == 1 { return 1; } return 0; } }")]
    [InlineData("System::Uri func(string a) { return new System::Uri(a); }")]
    [InlineData("int func() { while true { return 1; } }")]
    [InlineData("int func() { let i = 0; while i < 10 { i = i + 1; } return i; }")]
    [InlineData("int func() { unless false { return 1; } return 0; }")]
    [InlineData("int func() { unless false { return 1; } else { return 0; } }")]
    [InlineData("int func() { if true { return 1; } if true { return 2; } return 0; }")]
    [InlineData("int func() { if true { if false { return 1; } else { return 2; } } else { return 3; } }")]
    [InlineData("int func() { let i = 0; while i < 10 { while i < 5 { break; } i = i + 1; } return i; }")]
    [InlineData("int func() { let i = 0; while i < 10 { while i < 5 { continue; } i = i + 1; } return i; }")]
    [InlineData("int func() { let i = 0; while i < 1 { while i < 2 { while i < 3 { break; } } } return i; }")]
    [InlineData("int func() { let i = 0; while i < 10 : outer { while true : inner { break; } i = i + 1; } return i; }")]
    [InlineData("int func() { let i = 0; while i < 10 : outer { while i < 5 : inner { continue outer; } i = i + 1; } return i; }")]
    [InlineData("int func() { let i = 0; while i < 10 { break; } return i; }")]
    [InlineData("int func() { until false { return 1; } }")]
    public void TestControlFlowAnalysisBasic(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        BindMemberAndAnalyze<BoundFunctionMember>(inputText, diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();
    }

    [Theory]
    [InlineData("int func() { }")]
    [InlineData("int func() { int.MaxValue.ToString(); }")]
    public void TestControlFlowAnalysisWithNoReturnStatement(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        BindMemberAndAnalyze<BoundFunctionMember>(inputText, diagnosticBuilder);
        var diagnostics = diagnosticBuilder.Build().ToList();

        diagnostics.Count.Should().Be(1);
        diagnostics[0].ErrorCode.Should().Be(ErrorCode.NotAllPathsReturn);
        diagnostics[0].Level.Should().Be(DiagnosticLevel.Error);
    }

    [Theory]
    [InlineData("void func() { return; 10.ToString(); }")]
    [InlineData("int func() { return 10; 10.ToString(); }")]
    [InlineData("int func() { if true { return 10; 10.ToString();} return 0; }")]
    [InlineData("System::Uri func(string a) { const r = new System::Uri(a); return r; r.ToString(); }")]
    public void TestControlFlowAnalysisWithUnreachableCode(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        BindMemberAndAnalyze<BoundFunctionMember>(inputText, diagnosticBuilder);
        var diagnostics = diagnosticBuilder.Build().ToList();

        diagnostics[0].ErrorCode.Should().Be(ErrorCode.UnreachableCode);
        diagnostics[0].Level.Should().Be(DiagnosticLevel.Warning);
    }

    [Theory]
    [InlineData("int func() { if true { return 10; } }")]
    [InlineData("int func() { if true { } else { return 0; } }")]
    [InlineData("int func() { const a = 3; if a == 0 { return int.MaxValue; } else { if a == 1 { return 1; } } }")]
    public void TestControlFlowAnalysisWithConditionalStatements(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        BindMemberAndAnalyze<BoundFunctionMember>(inputText, diagnosticBuilder);
        var diagnostics = diagnosticBuilder.Build().ToList();

        diagnostics.Count.Should().Be(1);
        diagnostics[0].ErrorCode.Should().Be(ErrorCode.NotAllPathsReturn);
        diagnostics[0].Level.Should().Be(DiagnosticLevel.Error);
    }

    [Fact]
    public void BreakInInfiniteLoopShouldReportUnreachableCodeAndMissingReturn()
    {
        // `break` gives the loop a real, reachable exit path with no return after it -
        // both the dead code after `break` and the missing return must be reported.
        var diagnosticBuilder = new DiagnosticBag.Builder();
        BindMemberAndAnalyze<BoundFunctionMember>("int func() { while true { break; return 1; } }", diagnosticBuilder);
        var diagnostics = diagnosticBuilder.Build().ToList();

        diagnostics.Count.Should().Be(2);
        diagnostics.Should().Contain(d => d.ErrorCode == ErrorCode.UnreachableCode && d.Level == DiagnosticLevel.Warning);
        diagnostics.Should().Contain(d => d.ErrorCode == ErrorCode.NotAllPathsReturn && d.Level == DiagnosticLevel.Error);
    }

    [Fact]
    public void ContinueInInfiniteLoopShouldOnlyReportUnreachableCode()
    {
        // `continue` always re-enters the loop, so `while true { continue; ... }` never
        // reaches an exit; only the dead code after `continue` is reachable-code-wise wrong.
        var diagnosticBuilder = new DiagnosticBag.Builder();
        BindMemberAndAnalyze<BoundFunctionMember>("int func() { while true { continue; return 1; } }", diagnosticBuilder);
        var diagnostics = diagnosticBuilder.Build().ToList();

        diagnostics.Count.Should().Be(1);
        diagnostics[0].ErrorCode.Should().Be(ErrorCode.UnreachableCode);
        diagnostics[0].Level.Should().Be(DiagnosticLevel.Warning);
    }

    [Theory]
    [InlineData("int func() { if true { return 1; } else { return 0; } 10.ToString(); }")]
    [InlineData("int func() { const a = 1; if a == 0 { return 0; } else if a == 1 { return 1; } else { return 2; } 10.ToString(); }")]
    public void ControlFlowAnalysisWithUnreachableCodeAfterFullyReturningConditional(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        BindMemberAndAnalyze<BoundFunctionMember>(inputText, diagnosticBuilder);
        var diagnostics = diagnosticBuilder.Build().ToList();
        diagnostics.Count.Should().Be(1);

        diagnostics[0].ErrorCode.Should().Be(ErrorCode.UnreachableCode);
        diagnostics[0].Level.Should().Be(DiagnosticLevel.Warning);
    }

    [Theory]
    [InlineData("int func() { return 1; }")]
    [InlineData("int func() { if true { return 1; } else { return 0; } }")]
    [InlineData("int func() { let i = 0; while i < 10 { i = i + 1; } return i; }")]
    [InlineData("int func() { let i = 0; while i < 10 { while i < 5 { break; } i = i + 1; } return i; }")]
    [InlineData("int func() { let i = 0; while i < 10 { while i < 5 { continue; } i = i + 1; } return i; }")]
    [InlineData("int func() { let i = 0; while i < 1 { while i < 2 { while i < 3 { break; } } } return i; }")]
    [InlineData("int func() { let i = 0; while i < 10 { break; } return i; }")]
    [InlineData("int func() { let i = 0; while i < 10 : outer { while true : inner { break outer; } i = i + 1; } return i; }")]
    public void ControlFlowGraphShouldNotHaveDuplicateEdges(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundMember = TestUtils.BindMember<BoundFunctionMember>(inputText, diagnosticBuilder);
        var controlFlowGraph = ControlFlowGraph.Create(boundMember);

        foreach (var block in controlFlowGraph.Blocks)
        {
            var outgoingTargets = block.Outgoing.Select(b => b.To).ToList();
            outgoingTargets.Should().OnlyHaveUniqueItems($"block should not have duplicate outgoing edges");

            var incomingSources = block.Incoming.Select(b => b.From).ToList();
            incomingSources.Should().OnlyHaveUniqueItems($"block should not have duplicate incoming edges");
        }
    }

    [Theory]
    [InlineData("int func() { let i = 0; while i < 10 { i = i + 1; } return i; }")]
    [InlineData("int func() { let i = 0; while i < 10 { while i < 5 { break; } i = i + 1; } return i; }")]
    [InlineData("int func() { let i = 0; while i < 10 { while i < 5 { continue; } i = i + 1; } return i; }")]
    [InlineData("int func() { let i = 0; while i < 1 { while i < 2 { while i < 3 { break; } } } return i; }")]
    [InlineData("void func() { if true { } while true { } }")]
    [InlineData("int func() { let i = 0; while i < 10 : outer { while true : inner { break outer; } i = i + 1; } return i; }")]
    public void ControlFlowGraphBlocksShouldContainEveryReferencedBlock(string inputText)
    {
        // Every block a Branch points at or from must actually be present in Blocks;
        // otherwise a consumer walking Blocks (e.g. a future emitter) silently loses edges.
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundMember = TestUtils.BindMember<BoundFunctionMember>(inputText, diagnosticBuilder);
        var controlFlowGraph = ControlFlowGraph.Create(boundMember);

        var referencedBlocks = controlFlowGraph.Branches
            .SelectMany(branch => new[] { branch.From, branch.To })
            .Distinct();

        referencedBlocks.Should().BeSubsetOf(controlFlowGraph.Blocks);
    }

    [Theory]
    [InlineData("int func() { let i = 0; while i < 10 { i = i + 1; } return i; }")]
    [InlineData("int func() { let i = 0; while i < 10 { while i < 5 { continue; } i = i + 1; } return i; }")]
    [InlineData("int func() { let i = 0; while i < 1 { while i < 2 { while i < 3 { break; } } } return i; }")]
    public void ControlFlowGraphShouldProduceBackEdgesForLoops(string inputText)
    {
        // A loop that can iterate more than once must have at least one edge whose target
        // appears no later than its source in Blocks - i.e. an actual back edge, not just a
        // straight-line DAG from the loop header through to the function's end.
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundMember = TestUtils.BindMember<BoundFunctionMember>(inputText, diagnosticBuilder);
        var controlFlowGraph = ControlFlowGraph.Create(boundMember);

        var blockIndex = controlFlowGraph.Blocks
            .Select((block, index) => (block, index))
            .ToDictionary(x => x.block, x => x.index);

        var hasBackEdge = controlFlowGraph.Branches
            .Any(branch => blockIndex[branch.To] <= blockIndex[branch.From]);

        hasBackEdge.Should().BeTrue("a loop that iterates must have a branch back to an earlier block");
    }

    [Theory]
    [InlineData("void func() { break; }")]
    [InlineData("void func() { continue; }")]
    [InlineData("void func() { if true { break; } }")]
    public void ControlFlowAnalysisShouldNotThrowWhenBreakOrContinueHasNoEnclosingLoop(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();

        var act = () => BindMemberAndAnalyze<BoundFunctionMember>(inputText, diagnosticBuilder);

        act.Should().NotThrow();

        var diagnostics = diagnosticBuilder.Build().ToList();
        diagnostics.Count.Should().Be(1);
        diagnostics[0].ErrorCode.Should().Be(ErrorCode.NoEnclosingLoop);
        diagnostics[0].Level.Should().Be(DiagnosticLevel.Error);
    }

    [Theory]
    [InlineData("void func() { let i = 0; while i < 10 { break outer; } }")]
    [InlineData("void func() { let i = 0; while i < 10 { continue outer; } }")]
    public void ControlFlowAnalysisShouldNotThrowWhenLoopLabelIsUndefined(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();

        var act = () => BindMemberAndAnalyze<BoundFunctionMember>(inputText, diagnosticBuilder);

        act.Should().NotThrow();

        var diagnostics = diagnosticBuilder.Build().ToList();
        diagnostics.Count.Should().Be(1);
        diagnostics[0].ErrorCode.Should().Be(ErrorCode.UndefinedLoopLabel);
        diagnostics[0].Level.Should().Be(DiagnosticLevel.Error);
    }

    [Theory]
    [InlineData("int func() { return 1; if true { 2.ToString(); } }")]
    [InlineData("int func() { return 1; if true { } }")]
    public void ControlFlowAnalysisShouldNotThrowOnUnreachableSynthesizedBlock(string inputText)
    {
        // The synthesized placeholder for an empty/no-else branch carries no SyntaxNode;
        // reporting it as unreachable must fall back to a location instead of crashing.
        var diagnosticBuilder = new DiagnosticBag.Builder();

        var act = () => BindMemberAndAnalyze<BoundFunctionMember>(inputText, diagnosticBuilder);

        act.Should().NotThrow();

        var diagnostics = diagnosticBuilder.Build().ToList();
        diagnostics.Should().Contain(d => d.ErrorCode == ErrorCode.UnreachableCode && d.Level == DiagnosticLevel.Warning);
    }

    [Fact]
    public void ControlFlowAnalysisShouldReportMissingReturnWhenLoopCanExitWithoutReturning()
    {
        // The loop's zero-iteration fallthrough is a real path out of the function that
        // never reaches a return statement.
        var diagnosticBuilder = new DiagnosticBag.Builder();
        BindMemberAndAnalyze<BoundFunctionMember>("int func() { let i = 0; while i < 10 { return 1; } }", diagnosticBuilder);
        var diagnostics = diagnosticBuilder.Build().ToList();

        diagnostics.Count.Should().Be(1);
        diagnostics[0].ErrorCode.Should().Be(ErrorCode.NotAllPathsReturn);
        diagnostics[0].Level.Should().Be(DiagnosticLevel.Error);
    }

    [Fact]
    public void ControlFlowAnalysisShouldTrackVariableDeclarationsForReachability()
    {
        // Variable declarations must be visible to the CFG like any other statement.
        var diagnosticBuilder = new DiagnosticBag.Builder();
        BindMemberAndAnalyze<BoundFunctionMember>("int func() { return 1; let a = 2; }", diagnosticBuilder);
        var diagnostics = diagnosticBuilder.Build().ToList();

        diagnostics.Count.Should().Be(1);
        diagnostics[0].ErrorCode.Should().Be(ErrorCode.UnreachableCode);
        diagnostics[0].Level.Should().Be(DiagnosticLevel.Warning);
    }

    [Fact]
    public void ControlFlowAnalysisShouldResolveContinueToTheInnermostLoop()
    {
        // `continue` inside the inner loop must re-check the inner loop's own condition,
        // not fall through to outer-loop code - so only the code after `continue` in the
        // same block is unreachable, nothing in the outer loop is affected.
        var diagnosticBuilder = new DiagnosticBag.Builder();
        BindMemberAndAnalyze<BoundFunctionMember>(
            "int func() { let i = 0; while i < 1 { i.ToString(); while i < 2 { continue; i.ToString(); } } return i; }",
            diagnosticBuilder);
        var diagnostics = diagnosticBuilder.Build().ToList();

        diagnostics.Count.Should().Be(1);
        diagnostics[0].ErrorCode.Should().Be(ErrorCode.UnreachableCode);
        diagnostics[0].Level.Should().Be(DiagnosticLevel.Warning);
    }

    [Fact]
    public void ControlFlowAnalysisShouldResolveLabeledBreakToTheTargetLoop()
    {
        // `break outer;` must exit the *outer* loop directly. The inner loop's condition is
        // a constant `true` with no break of its own, so its exit is unreachable - meaning
        // the only way out is the labeled break, and the code between the inner loop and
        // the end of the outer loop's body is dead. A bare `break;` here (see the
        // `TestControlFlowAnalysisBasic` case with the same shape) would instead exit only
        // the inner loop, leaving `i = i + 1;` reachable.
        var diagnosticBuilder = new DiagnosticBag.Builder();
        BindMemberAndAnalyze<BoundFunctionMember>(
            "int func() { let i = 0; while i < 10 : outer { while true : inner { break outer; } i = i + 1; } return i; }",
            diagnosticBuilder);
        var diagnostics = diagnosticBuilder.Build().ToList();

        diagnostics.Count.Should().Be(1);
        diagnostics[0].ErrorCode.Should().Be(ErrorCode.UnreachableCode);
        diagnostics[0].Level.Should().Be(DiagnosticLevel.Warning);
    }

    private static TBoundMember BindMemberAndAnalyze<TBoundMember>(string inputText, DiagnosticBag.Builder diagnosticBuilder) where TBoundMember : BoundMember
    {
        var boundMember = TestUtils.BindMember<TBoundMember>(inputText, diagnosticBuilder);
        new ControlFlowAnalyzer(diagnosticBuilder).Visit(boundMember);
        return boundMember;
    }
}
