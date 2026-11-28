using System.Collections.Generic;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class TypedVariableDeclarationTests
{
    public static IEnumerable<object[]> Cases()
    {
        yield return new object[] { "long Run() { const long a = 5L; return a; }", 5L };
        yield return new object[] { "int Run() { let int count = 1; count += 2; return count; }", 3 };
        yield return new object[] { "int Run() { let System::Int32 y = 2; return y * 4; }", 8 };
        yield return new object[] { "string Run() { const string[] xs = [\"a\", \"b\"]; return xs[1]; }", "b" };
        yield return new object[] { "long Run() { const inferred = 42L; return inferred; }", 42L };
        yield return new object[] { "const long Offset = 10L; long Run() { return Offset; }", 10L };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TypedDeclarationsShouldProduceExpectedResult(string source, object expected)
    {
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString($"{source}\nvoid Main() {{}}"));
        diagnostics.Should().BeEmpty();
        assemblyDefinition.Should().NotBeNull();

        object result = null;
        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            result = assembly.EntryPoint.DeclaringType.GetMethod("Run").Invoke(null, null);
        });

        result.Should().Be(expected);
    }
}
