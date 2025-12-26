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
    [InlineData("int func() { return int.MaxValue; }")]
    [InlineData("int func() { int.MaxValue.ToString(); return int.MaxValue; }")]
    [InlineData("int func() { if true { return int.MaxValue; } return 0; }")]
    [InlineData("int func() { if true { return int.MaxValue; } else { return 0; } }")]
    [InlineData("int func() { if true { } return 0; }")]
    [InlineData("int func() { const a = 3; if a == 0 { return int.MaxValue; } else if a == 1 { return 1; } else { return 0; } }")]
    [InlineData("int func() { const a = 3; if a == 0 { return int.MaxValue; } else { if a == 1 { return 1; } return 0; } }")]
    [InlineData("System.Uri func(string a) { return new System.Uri(a); }")]
    [InlineData("int func() { while true { return 1; } }")]
    [InlineData("int func() { let i = 0; while i < 10 { i = i + 1; } return i; }")]
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

        diagnostics[0].ErrorCode.Should().Be(ErrorCode.NotAllPathsReturn);
        diagnostics[0].Level.Should().Be(DiagnosticLevel.Error);
    }

    [Theory]
    [InlineData("void func() { return; 10.ToString(); }")]
    [InlineData("int func() { return 10; 10.ToString(); }")]
    [InlineData("int func() { if true { return 10; 10.ToString();} return 0; }")]
    [InlineData("System.Uri func(string a) { const r = new System.Uri(a); return r; r.ToString(); }")]
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

        diagnostics[0].ErrorCode.Should().Be(ErrorCode.NotAllPathsReturn);
        diagnostics[0].Level.Should().Be(DiagnosticLevel.Error);
    }

    [Theory]
    [InlineData("int func() { while true { break; return 1; } }")]
    [InlineData("int func() { while true { continue; return 1; } }")]
    public void TestControlFlowAnalysisWithLoopStatements(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        BindMemberAndAnalyze<BoundFunctionMember>(inputText, diagnosticBuilder);
        var diagnostics = diagnosticBuilder.Build().ToList();
        diagnostics.Should().NotBeEmpty();
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
