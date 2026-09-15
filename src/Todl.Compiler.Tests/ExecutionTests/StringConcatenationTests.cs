using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class StringConcatenationTests
{
    [Theory]
    [InlineData(
        "string Run(string a, string b) { return a + b; }",
        new object[] { "foo", "bar" },
        "foobar")]
    [InlineData(
        "string Run(string a, string b, string c) { return a + b + c; }",
        new object[] { "foo", "bar", "baz" },
        "foobarbaz")]
    [InlineData(
        "string Run(string a, string b) { return \"[\" + a + \"]-[\" + b + \"]\"; }",
        new object[] { "foo", "bar" },
        "[foo]-[bar]")]
    public void StringConcatenationShouldEmitAndRunSuccessfully(string functionText, object[] arguments, string expected)
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

    [Fact]
    public void MoreThanFourOperandsShouldEmitAndRunSuccessfully()
    {
        // Regression test for the >4-operand fallback path.
        var inputText = @"
import { Console } from System;

void Main() {}
string Run(string a, string b, string c, string d, string e) {
    return a + b + c + d + e;
}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        diagnostics.Should().BeEmpty();
        assemblyDefinition.Should().NotBeNull();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();
            runMethod.Invoke(null, new object[] { "1", "2", "3", "4", "5" }).Should().Be("12345");
        });
    }
}
