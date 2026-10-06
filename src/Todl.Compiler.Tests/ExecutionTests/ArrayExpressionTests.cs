using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class ArrayExpressionTests
{
    private static object Run(string body, params object[] args)
    {
        var inputText = $"{body}\nvoid Main() {{}}";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        diagnostics.Should().BeEmpty();
        assemblyDefinition.Should().NotBeNull();

        object result = null;
        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType.GetMethod("Run");
            result = runMethod.Invoke(null, args);
        });

        return result;
    }

    [Fact]
    public void CreatedArrayShouldBeZeroInitializedAndWritable()
    {
        Run("int Run() { let a = new int[3]; a[1] = 7; return a[0] + a[1] + a[2]; }").Should().Be(7);
    }

    [Fact]
    public void ArrayLiteralShouldStoreElementsInOrder()
    {
        Run("int Run() { let a = [10, 20, 30]; return a[0] * 100 + a[2]; }").Should().Be(1030);
    }

    [Fact]
    public void NestedArrayLiteralShouldBeIndexable()
    {
        Run("int Run() { let a = [[1, 2], [3, 4, 5]]; return a[1][2]; }").Should().Be(5);
    }

    [Fact]
    public void JaggedArrayCreationShouldAllowAssigningInnerArrays()
    {
        Run("int Run() { let a = new int[2][]; a[1] = [4, 6]; return a[1][1]; }").Should().Be(6);
    }

    [Fact]
    public void CompoundAssignmentShouldEvaluateBaseAndIndexOnce()
    {
        Run("int Run() { let a = [1, 2, 3]; a[2] += 10; a[0] *= 5; return a[0] + a[2]; }").Should().Be(18);
    }

    [Fact]
    public void StringArrayAndReferenceElementsShouldRoundTrip()
    {
        Run("string Run() { let a = new string[2]; a[1] = \"x\"; return a[1]; }").Should().Be("x");
    }

    [Fact]
    public void LongAndDoubleElementsShouldRoundTrip()
    {
        Run("long Run() { let a = [1L, 2L]; return a[1]; }").Should().Be(2L);
        Run("double Run() { let a = [1.5, 2.5]; return a[1]; }").Should().Be(2.5);
    }

    [Fact]
    public void ArrayParameterShouldBeReadable()
    {
        Run("string Run(string[] args) { return args[1]; }", new object[] { new[] { "a", "b" } }).Should().Be("b");
    }

    [Fact]
    public void ClrIndexerShouldBeReadable()
    {
        Run("char Run(string s) { return s[1]; }", "abc").Should().Be('b');
    }

    [Fact]
    public void ClrIndexerShouldBeWritable()
    {
        Run("import { ArrayList } from System::Collections;\nimport { Object } from System;\nObject Run() { let l = new ArrayList(); let n = l.Add(\"a\"); l[0] = \"b\"; return l[0]; }")
            .Should().Be("b");
    }
}
