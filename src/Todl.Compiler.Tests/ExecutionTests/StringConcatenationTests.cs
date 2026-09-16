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
    [InlineData(
        "string Run(int a) { return \"n: \" + a; }",
        new object[] { 5 },
        "n: 5")]
    [InlineData(
        "string Run(int a) { return a + \" apples\"; }",
        new object[] { 5 },
        "5 apples")]
    [InlineData(
        "string Run(bool a) { return \"flag: \" + a; }",
        new object[] { true },
        "flag: True")]
    [InlineData(
        "string Run(double a) { return \"pi=\" + a + \"!\"; }",
        new object[] { 3.5 },
        "pi=3.5!")]
    [InlineData(
        "string Run(int a, int b) { return \"sum: \" + (a + b); }",
        new object[] { 2, 3 },
        "sum: 5")]
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
