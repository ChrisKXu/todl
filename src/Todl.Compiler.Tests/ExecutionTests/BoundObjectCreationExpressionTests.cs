using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class BoundObjectCreationExpressionTests
{
    [Fact]
    public void NewExpressionShouldEmitAndConstructTheClrType()
    {
        var inputText = @"
import { Exception } from System;

void Main() {}
string Run() {
    let e = new Exception(""boom"");
    return e.ToString();
}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        diagnostics.Should().BeEmpty();
        assemblyDefinition.Should().NotBeNull();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();

            runMethod.Invoke(null, null).Should().Be("System.Exception: boom");
        });
    }
}
