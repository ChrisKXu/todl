using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
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
    [InlineData("int func() { return int.MaxValue; }")]
    [InlineData("int func() { int.MaxValue.ToString(); return int.MaxValue; }")]
    [InlineData("System.Uri func(string a) { return new System.Uri(a); }")]
    public void TestControlFlowAnalysisBasic(string inputText)
    {
        var syntaxTree = SyntaxTree.Parse(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache);
        BoundModule.Create(new[] { syntaxTree }).GetDiagnostics().Should().BeEmpty();
    }

    [Theory]
    [InlineData("int func() { }")]
    [InlineData("int func() { int.MaxValue.ToString(); }")]
    public void TestControlFlowAnalysisWithNoReturnStatement(string inputText)
    {
        var syntaxTree = SyntaxTree.Parse(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache);
        var module = BoundModule.Create(new[] { syntaxTree });
        var diagnostics = module.GetDiagnostics().ToList();

        diagnostics[0].ErrorCode.Should().Be(ErrorCode.NotAllPathsReturn);
        diagnostics[0].Level.Should().Be(DiagnosticLevel.Error);
    }

    [Theory]
    [InlineData("void func() { return; 10.ToString(); }")]
    [InlineData("int func() { return 10; 10.ToString(); }")]
    [InlineData("System.Uri func(string a) { const r = new System.Uri(a); return r; r.ToString(); }")]
    public void TestControlFlowAnalysisWithUnreachableCode(string inputText)
    {
        var syntaxTree = SyntaxTree.Parse(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache);
        var module = BoundModule.Create(new[] { syntaxTree });
        var diagnostics = module.GetDiagnostics().ToList();

        diagnostics[0].ErrorCode.Should().Be(ErrorCode.UnreachableCode);
        diagnostics[0].Level.Should().Be(DiagnosticLevel.Warning);
    }
}
