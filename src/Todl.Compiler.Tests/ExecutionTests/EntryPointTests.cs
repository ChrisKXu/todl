using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class EntryPointTests
{
    [Fact]
    public void TestVoidMainWithEmptyArgs()
    {
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString("void Main() {}"));

        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        var entryPoint = assemblyDefinition.MainModule.EntryPoint;
        entryPoint.Should().NotBeNull();
        entryPoint.Parameters.Count.Should().Be(0);
        entryPoint.ReturnType.Should().Be(assemblyDefinition.MainModule.TypeSystem.Void);
        entryPoint.HasBody.Should().BeTrue();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var result = assembly.EntryPoint.Invoke(null, null);
            result.Should().BeNull();
        });
    }

    [Fact]
    public void TestIntMainWithEmptyArgs()
    {
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString("int Main() { return 0; }"));

        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        var entryPoint = assemblyDefinition.MainModule.EntryPoint;
        entryPoint.Should().NotBeNull();
        entryPoint.Parameters.Count.Should().Be(0);
        entryPoint.ReturnType.Should().Be(assemblyDefinition.MainModule.TypeSystem.Int32);
        entryPoint.HasBody.Should().BeTrue();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var result = assembly.EntryPoint.Invoke(null, null);
            result.Should().Be(0);
        });
    }

    [Fact]
    public void TestVoidMainWithStringArrayArgs()
    {
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString("void Main(string[] args) {}"));

        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        var entryPoint = assemblyDefinition.MainModule.EntryPoint;
        entryPoint.Should().NotBeNull();
        entryPoint.Parameters.Count.Should().Be(1);
        entryPoint.ReturnType.Should().Be(assemblyDefinition.MainModule.TypeSystem.Void);
        entryPoint.HasBody.Should().BeTrue();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var result = assembly.EntryPoint.Invoke(null, new object[] { new[] { "hello", "world" } });
            result.Should().BeNull();
        });
    }

    [Fact]
    public void TestIntMainWithStringArrayArgs()
    {
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString("int Main(string[] args) { return 0; }"));

        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        var entryPoint = assemblyDefinition.MainModule.EntryPoint;
        entryPoint.Should().NotBeNull();
        entryPoint.Parameters.Count.Should().Be(1);
        entryPoint.ReturnType.Should().Be(assemblyDefinition.MainModule.TypeSystem.Int32);
        entryPoint.HasBody.Should().BeTrue();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var result = assembly.EntryPoint.Invoke(null, new object[] { new[] { "hello", "world" } });
            result.Should().Be(0);
        });
    }
}
