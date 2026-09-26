using System;
using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class BoundLoopStatementTests
{
    [Fact]
    public void LargeLoopBodyShouldEmitAndRunSuccessfully()
    {
        // Regression test: the loop's back-edge condition check always emitted Brtrue_S/
        // Brfalse_S (a 1-byte signed offset, range -128..127). Mono.Cecil does not auto-widen
        // short-form branches, so once a loop body's compiled IL exceeds ~127 bytes the
        // offset silently overflows and corrupts the branch target, producing an
        // InvalidProgramException (or, if the corrupted target happens to land on valid code,
        // wrong behavior) at runtime. 40 repeated statements comfortably exceed that budget.
        var incrementStatements = string.Concat(Enumerable.Repeat("        sum = sum + 1;\n", 40));
        var inputText = $@"
import {{ Console }} from System;

void Main() {{}}
int Run() {{
    let sum = 0;
    let i = 0;
    while i < 3 {{
{incrementStatements}
        i = i + 1;
    }}
    return sum;
}}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        diagnostics.Should().BeEmpty();
        assemblyDefinition.Should().NotBeNull();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();
            runMethod.Invoke(null, null).Should().Be(120);
        });
    }
}
