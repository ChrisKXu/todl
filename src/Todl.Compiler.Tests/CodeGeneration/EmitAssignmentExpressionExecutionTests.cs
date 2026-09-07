using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.CodeGeneration;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

// IL-shape assertions alone missed a real bug here: EmitAssignmentExpression applied the
// operator without first loading the target's current value, so e.g. `a -= 10` computed
// `10 - <nothing>` (stack underflow -> InvalidProgramException at runtime). This proves the
// actual computed result, not just the opcode sequence.
public sealed class EmitAssignmentExpressionExecutionTests
{
    [Fact]
    public void InlineAssignmentOperatorsComputeAgainstTheCurrentValue()
    {
        var inputText = @"
void Main() {}
int Run() {
    let a = 5;
    a += 3;
    a -= 2;
    a *= 4;
    a /= 3;
    return a;
}
";
        var compilation = new Compilation(
            assemblyName: "test",
            version: new Version(1, 0),
            sourceTexts: new[] { SourceText.FromString(inputText) },
            metadataLoadContext: TestDefaults.MetadataLoadContext);

        var assemblyDefinition = compilation.Emit();
        compilation.GetDiagnostics().Should().BeEmpty();

        using var memoryStream = new MemoryStream();
        assemblyDefinition.Write(memoryStream);
        var assembly = Assembly.Load(memoryStream.GetBuffer());

        var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
        runMethod.Should().NotBeNull();

        // ((5 + 3 - 2) * 4) / 3 = 8
        runMethod.Invoke(null, Array.Empty<object>()).Should().Be(8);
    }
}
