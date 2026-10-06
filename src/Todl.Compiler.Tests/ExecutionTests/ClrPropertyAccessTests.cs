using System.Collections.Generic;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class ClrPropertyAccessTests
{
    private const string Imports
        = "import { TestClass } from Todl::Compiler::Tests;\nimport { Environment } from System;\n";

    public static IEnumerable<object[]> Cases()
    {
        // source, arguments passed to Run, expected result
        yield return new object[] { "int Run(string s) { return s.Length; }", new object[] { "abc" }, 3 };
        yield return new object[] { "int Run() { let a = new int[4]; return a.Length; }", null, 4 };
        yield return new object[] { "long Run() { let a = [1, 2]; return a.LongLength; }", null, 2L };
        yield return new object[] { Imports + "string Run() { return Environment.NewLine; }", null, System.Environment.NewLine };
        yield return new object[] { Imports + "int Run() { return Environment.ProcessorCount; }", null, System.Environment.ProcessorCount };
        yield return new object[] { Imports + "int Run() { TestClass.PublicStaticIntProperty = 41; return TestClass.PublicStaticIntProperty; }", null, 41 };
        yield return new object[] { Imports + "string Run() { TestClass.PublicStaticStringProperty = \"v\"; return TestClass.PublicStaticStringProperty; }", null, "v" };
        yield return new object[] { Imports + "bool Run() { TestClass.PublicStaticBoolProperty = true; return TestClass.PublicStaticBoolProperty; }", null, true };
        yield return new object[] { Imports + "int Run() { let t = new TestClass(); t.PublicIntProperty = 7; return t.PublicIntProperty; }", null, 7 };
        yield return new object[] { Imports + "string Run() { let t = new TestClass(); t.PublicStringProperty = \"w\"; return t.PublicStringProperty; }", null, "w" };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ClrPropertyProgramShouldProduceExpectedResult(string source, object[] args, object expected)
    {
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString($"{source}\nvoid Main() {{}}"));
        diagnostics.Should().BeEmpty();
        assemblyDefinition.Should().NotBeNull();

        object result = null;
        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            result = assembly.EntryPoint.DeclaringType.GetMethod("Run").Invoke(null, args);
        });

        result.Should().Be(expected);
    }
}
