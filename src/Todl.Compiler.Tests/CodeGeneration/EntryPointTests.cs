using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

    private static void RunAssembly(AssemblyDefinition assemblyDefinition, Action<Assembly> action)
    {
        using var memoryStream = new MemoryStream();
        assemblyDefinition.Write(memoryStream);
        var assembly = Assembly.Load(memoryStream.GetBuffer());

        action(assembly);
    }

    [Fact]
    public void TestVoidMainWithEmptyArgs()
    {
        var (assemblyDefinition, diagnostics) = Compile(SourceText.FromString("void Main() {}"));

        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        var entryPoint = assemblyDefinition.MainModule.EntryPoint;
        entryPoint.Should().NotBeNull();
        entryPoint.Parameters.Count.Should().Be(0);
        entryPoint.ReturnType.Should().Be(assemblyDefinition.MainModule.TypeSystem.Void);
        entryPoint.HasBody.Should().BeTrue();

        RunAssembly(assemblyDefinition, assembly =>
        {
            var result = assembly.EntryPoint.Invoke(null, null);
            result.Should().BeNull();
        });
    }

    [Fact]
    public void TestIntMainWithEmptyArgs()
    {
        var (assemblyDefinition, diagnostics) = Compile(SourceText.FromString("int Main() { return 0; }"));

        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        var entryPoint = assemblyDefinition.MainModule.EntryPoint;
        entryPoint.Should().NotBeNull();
        entryPoint.Parameters.Count.Should().Be(0);
        entryPoint.ReturnType.Should().Be(assemblyDefinition.MainModule.TypeSystem.Int32);
        entryPoint.HasBody.Should().BeTrue();

        RunAssembly(assemblyDefinition, assembly =>
        {
            var result = assembly.EntryPoint.Invoke(null, null);
            result.Should().Be(0);
        });
    }

    [Fact]
    public void TestVoidMainWithStringArrayArgs()
    {
        var (assemblyDefinition, diagnostics) = Compile(SourceText.FromString("void Main(string[] args) {}"));

        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        var entryPoint = assemblyDefinition.MainModule.EntryPoint;
        entryPoint.Should().NotBeNull();
        entryPoint.Parameters.Count.Should().Be(1);
        entryPoint.ReturnType.Should().Be(assemblyDefinition.MainModule.TypeSystem.Void);
        entryPoint.HasBody.Should().BeTrue();

        RunAssembly(assemblyDefinition, assembly =>
        {
            var result = assembly.EntryPoint.Invoke(null, new object[] { new[] { "hello", "world" } });
            result.Should().BeNull();
        });
    }

    [Fact]
    public void TestIntMainWithStringArrayArgs()
    {
        var (assemblyDefinition, diagnostics) = Compile(SourceText.FromString("int Main(string[] args) { return 0; }"));

        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        var entryPoint = assemblyDefinition.MainModule.EntryPoint;
        entryPoint.Should().NotBeNull();
        entryPoint.Parameters.Count.Should().Be(1);
        entryPoint.ReturnType.Should().Be(assemblyDefinition.MainModule.TypeSystem.Int32);
        entryPoint.HasBody.Should().BeTrue();

        RunAssembly(assemblyDefinition, assembly =>
        {
            var result = assembly.EntryPoint.Invoke(null, new object[] { new[] { "hello", "world" } });
            result.Should().Be(0);
        });
    }
}
