using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class BoundReturnStatementTests
{
    [Fact]
    public void MultipleValuedReturnsInsideBranchesAndLoops()
    {
        var inputText = @"
void Main() {}
int Run(int i) {
    if i < 0 { return -1; }

    let n = 0;
    while n < 10 {
        if n == i { return n * 10; }
        n = n + 1;
    }

    return -2;
}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();

            runMethod.Invoke(null, new object[] { -5 }).Should().Be(-1);
            runMethod.Invoke(null, new object[] { 0 }).Should().Be(0);
            runMethod.Invoke(null, new object[] { 4 }).Should().Be(40);
            runMethod.Invoke(null, new object[] { 50 }).Should().Be(-2);
        });
    }

    [Fact]
    public void MultipleVoidReturnsInsideBranchesAndLoops()
    {
        var inputText = @"
void Main() {}
void Run(int i) {
    if i < 0 { return; }

    let n = 0;
    while true {
        if n == i { return; }
        n = n + 1;
    }
}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();

            runMethod.Invoke(null, new object[] { -1 }).Should().BeNull();
            runMethod.Invoke(null, new object[] { 3 }).Should().BeNull();
        });
    }

    [Fact]
    public void FunctionEndingInReturnInsideIfElseStillRuns()
    {
        var inputText = @"
void Main() {}
int Run(int i) {
    if i == 1 { return 10; } else { return 20; }
}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();

            runMethod.Invoke(null, new object[] { 1 }).Should().Be(10);
            runMethod.Invoke(null, new object[] { 2 }).Should().Be(20);
        });
    }
}
