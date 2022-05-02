using System;
using System.Collections.Generic;
using FluentAssertions;
using Mono.Cecil;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.CodeGeneration;
using Todl.Compiler.Diagnostics;
using Todl.Compiler.Tests.CodeAnalysis;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EntryPointTests
{
    private static (AssemblyDefinition, IEnumerable<Diagnostic>) Compile(SourceText sourceText)
    {
        var compilation = new Compilation(
            assemblyName: "test",
            version: new Version(1, 0),
            sourceTexts: new[] { sourceText },
            assemblyPaths: TestDefaults.AssemblyPaths);

        return (compilation.Emit(), compilation.GetDiagnostics());
    }

    [Fact]
    public void TestVoidMainWithEmptyArgs()
    {
        var (assemblyDefinition, diagnostics) = Compile(SourceText.FromString("void Main() {}"));

        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        var entryPoint = assemblyDefinition.MainModule.EntryPoint;
        entryPoint.Should().NotBeNull();
        entryPoint.ReturnType.Should().Be(assemblyDefinition.MainModule.TypeSystem.Void);
        entryPoint.HasBody.Should().BeTrue();
    }

    [Fact]
    public void TestIntMainWithEmptyArgs()
    {
        var (assemblyDefinition, diagnostics) = Compile(SourceText.FromString("int Main() { return 0; }"));

        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        var entryPoint = assemblyDefinition.MainModule.EntryPoint;
        entryPoint.Should().NotBeNull();
        entryPoint.ReturnType.Should().Be(assemblyDefinition.MainModule.TypeSystem.Int32);
        entryPoint.HasBody.Should().BeTrue();
    }
}
