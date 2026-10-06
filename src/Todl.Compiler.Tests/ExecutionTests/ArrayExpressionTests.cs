using System.Collections.Generic;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class ArrayExpressionTests
{
    private const string ArrayListImports
        = "import { ArrayList } from System::Collections;\nimport { Object } from System;\n";

    public static IEnumerable<object[]> Cases()
    {
        // source, arguments passed to Run, expected result
        yield return new object[] { "int Run() { let a = new int[3]; a[1] = 7; return a[0] + a[1] + a[2]; }", null, 7 };
        yield return new object[] { "int Run() { let a = [10, 20, 30]; return a[0] * 100 + a[2]; }", null, 1030 };
        yield return new object[] { "int Run() { let a = [[1, 2], [3, 4, 5]]; return a[1][2]; }", null, 5 };
        yield return new object[] { "int Run() { let a = new int[2][]; a[1] = [4, 6]; return a[1][1]; }", null, 6 };
        yield return new object[] { "int Run() { let a = [1, 2, 3]; a[2] += 10; a[0] *= 5; return a[0] + a[2]; }", null, 18 };
        yield return new object[] { "string Run() { let a = new string[2]; a[1] = \"x\"; return a[1]; }", null, "x" };
        yield return new object[] { "long Run() { let a = [1L, 2L]; return a[1]; }", null, 2L };
        yield return new object[] { "double Run() { let a = [1.5, 2.5]; return a[1]; }", null, 2.5 };
        yield return new object[] { "string Run(string[] args) { return args[1]; }", new object[] { new[] { "a", "b" } }, "b" };
        yield return new object[] { "char Run(string s) { return s[1]; }", new object[] { "abc" }, 'b' };
        yield return new object[] { "int Run() { let a = new int[4]; return a.Length; }", null, 4 };
        yield return new object[] { "int Run() { let a = [[1], [2, 3]]; return a[1].Length; }", null, 2 };
        yield return new object[]
        {
            ArrayListImports + "Object Run() { let l = new ArrayList(); let n = l.Add(\"a\"); l[0] = \"b\"; return l[0]; }",
            null,
            "b"
        };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ArrayProgramShouldProduceExpectedResult(string source, object[] args, object expected)
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
