using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Mono.Cecil;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.CodeGeneration;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class BoundConditionalStatementTests
{
    private static (AssemblyDefinition, IEnumerable<Diagnostic>) Compile(SourceText sourceText)
    {
        var compilation = new Compilation(
            assemblyName: "test",
            version: new Version(1, 0),
            sourceTexts: new[] { sourceText },
            metadataLoadContext: TestDefaults.MetadataLoadContext);

        return (compilation.Emit(), compilation.GetDiagnostics());
    }

    private static void RunAssembly(AssemblyDefinition assemblyDefinition, Action<Assembly> action)
    {
        using var memoryStream = new MemoryStream();
        assemblyDefinition.Write(memoryStream);
        var assembly = Assembly.Load(memoryStream.GetBuffer());

        action(assembly);
    }

    [Fact]
    public void TestBoundConditionalStatement()
    {
        var inputText = @"
void Main() {}
int Run(int i) {
    if i == 1 { return 1; }

    return 0;
}
";
        var (assemblyDefinition, diagnostics) = Compile(SourceText.FromString(inputText));
        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();

            runMethod.Invoke(null, new object[] { 1 }).Should().Be(1);
            runMethod.Invoke(null, new object[] { 2 }).Should().Be(0);
        });
    }
}
