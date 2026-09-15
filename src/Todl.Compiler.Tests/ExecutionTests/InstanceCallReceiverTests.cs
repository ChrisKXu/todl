using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class InstanceCallReceiverTests
{
    [Fact]
    public void CallingInstanceMethodOnValueTypeExpressionResultShouldEmitAndRunSuccessfully()
    {
        // Regression test: value-type receivers with no stable storage crashed at runtime.
        var inputText = @"
import { Console } from System;

void Main() {}
string Run(int a, int b) {
    return (a + b).ToString();
}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        diagnostics.Should().BeEmpty();
        assemblyDefinition.Should().NotBeNull();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();
            runMethod.Invoke(null, [1, 2]).Should().Be("3");
        });
    }
}
