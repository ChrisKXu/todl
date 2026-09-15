using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class InstanceCallReceiverTests
{
    [Fact]
    public void CallingInstanceMethodOnValueTypeExpressionResultShouldEmitAndRunSuccessfully()
    {
        // Regression test: EmitInstanceCallReceiver only took the address of a value-type
        // receiver when it was a local or a parameter. For any other value-type-resultant
        // expression (here, the result of a binary addition), it pushed the raw value instead
        // of a managed pointer, which is invalid input to `call` and crashed at runtime with a
        // NullReferenceException inside Int32.ToString().
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
