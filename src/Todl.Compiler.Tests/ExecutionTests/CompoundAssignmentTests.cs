using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class CompoundAssignmentTests
{
    [Theory]
    [InlineData("int Run() { let a = 1; a += 2; return a; }", null, 3)]
    [InlineData("string Run() { let a = \"a\"; a += \"b\"; return a; }", null, "ab")]
    [InlineData("string Run() { let a = \"a\"; a += 1; return a; }", null, "a1")]
    [InlineData("string Run() { let a = \"a\"; a += true; return a; }", null, "aTrue")]
    [InlineData("string Run() { let a = \"a\"; a += \"b\" + \"c\"; return a; }", null, "abc")]
    [InlineData("string Run() { let a = [\"a\"]; a[0] += \"b\"; return a[0]; }", null, "ab")]
    [InlineData("string Run() { let a = [\"a\"]; a[0] += 1; return a[0]; }", null, "a1")]
    public void CompoundAssignmentShouldEmitAndRunSuccessfully(string functionText, object[] arguments, object expected)
    {
        var inputText = $@"
import {{ Console }} from System;

void Main() {{}}
{functionText}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        diagnostics.Should().BeEmpty();
        assemblyDefinition.Should().NotBeNull();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();
            runMethod.Invoke(null, arguments).Should().Be(expected);
        });
    }
}
