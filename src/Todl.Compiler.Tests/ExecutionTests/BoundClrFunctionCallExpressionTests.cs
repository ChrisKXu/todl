using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class BoundClrFunctionCallExpressionTests
{
    [Fact]
    public void CovariantOverloadMatchShouldEmitTheDeclaredParameterTypes()
    {
        var inputText = @"
import { Object } from System;

void Main() {}
bool Run(string a, string b) {
    return Object.ReferenceEquals(a, b);
}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        diagnostics.Should().BeEmpty();
        assemblyDefinition.Should().NotBeNull();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();

            var same = "same-instance-marker";
            runMethod.Invoke(null, new object[] { same, same }).Should().Be(true);

            var distinctA = new string(['d', 'i', 's', 't', 'i', 'n', 'c', 't']);
            var distinctB = new string(['d', 'i', 's', 't', 'i', 'n', 'c', 't']);
            runMethod.Invoke(null, new object[] { distinctA, distinctB }).Should().Be(false);
        });
    }

    [Fact]
    public void ClosedGenericReturnTypeShouldBindAndEmitSuccessfully()
    {
        var inputText = @"
import { Directory } from System::IO;

void Main() {}
int Run() {
    let entries = Directory.EnumerateFiles(""."");
    return 1;
}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        diagnostics.Should().BeEmpty();
        assemblyDefinition.Should().NotBeNull();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();

            runMethod.Invoke(null, null).Should().Be(1);
        });
    }

    [Fact]
    public void ArrayOfPrimitiveReturnTypeShouldBindAndEmitSuccessfully()
    {
        var inputText = @"
import { Environment } from System;

void Main() {}
int Run() {
    let args = Environment.GetCommandLineArgs();
    return 1;
}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        diagnostics.Should().BeEmpty();
        assemblyDefinition.Should().NotBeNull();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();

            runMethod.Invoke(null, null).Should().Be(1);
        });
    }
}
