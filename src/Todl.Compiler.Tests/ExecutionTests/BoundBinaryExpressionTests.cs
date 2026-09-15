using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class BoundBinaryExpressionTests
{
    [Fact]
    public void MultiplicationAndDivisionShouldEmitAndRunSuccessfully()
    {
        // Regression test: NumericMultiplication/NumericDivision bound successfully (and were
        // already handled by the evaluator and constant folder) but had no case in the IL
        // emitter's binary-operator switch, so `*`/`/` silently emitted no opcode at all,
        // leaving an unbalanced evaluation stack and an InvalidProgramException at runtime.
        var inputText = @"
import { Console } from System;

void Main() {}
int Run(int a, int b) {
    let product = a * b;
    let quotient = product / a;
    return product + quotient;
}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        diagnostics.Should().BeEmpty();
        assemblyDefinition.Should().NotBeNull();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();
            runMethod.Invoke(null, new object[] { 6, 7 }).Should().Be(49);
        });
    }

    [Theory]
    [InlineData("<", 2, 5, true)]
    [InlineData("<", 5, 2, false)]
    [InlineData("<", 3, 3, false)]
    [InlineData("<=", 3, 3, true)]
    [InlineData("<=", 5, 3, false)]
    [InlineData(">", 5, 2, true)]
    [InlineData(">", 2, 5, false)]
    [InlineData(">=", 3, 3, true)]
    [InlineData(">=", 2, 3, false)]
    public void RelationalOperatorsShouldEmitTheirOwnOpcodeNotAlwaysGreaterThan(string op, int a, int b, bool expected)
    {
        // Regression test: `<`, `<=`, `>`, and `>=` all bind to the same generic
        // BoundBinaryOperatorKind.Comparison, but the emitter unconditionally emitted Cgt
        // (greater-than) for every one of them - so `a < b` silently ran as `a > b`.
        var inputText = $@"
import {{ Console }} from System;

void Main() {{}}
bool Run(int a, int b) {{
    return a {op} b;
}}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        diagnostics.Should().BeEmpty();
        assemblyDefinition.Should().NotBeNull();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();
            runMethod.Invoke(null, new object[] { a, b }).Should().Be(expected);
        });
    }
}
