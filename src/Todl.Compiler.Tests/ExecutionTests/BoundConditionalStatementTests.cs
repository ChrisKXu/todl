using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class BoundConditionalStatementTests
{
    [Fact]
    public void TestBoundConditionalStatement()
    {
        var inputText = @"
void Main() {}
int Run(int i) {
    if i == 1 { return 1; }

    return 0;
}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();

            runMethod.Invoke(null, new object[] { 1 }).Should().Be(1);
            runMethod.Invoke(null, new object[] { 2 }).Should().Be(0);
        });
    }
}
