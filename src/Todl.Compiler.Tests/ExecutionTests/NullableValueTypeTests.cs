using System.Collections.Generic;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class NullableValueTypeTests
{
    private const string Imports
        = "import { NullableTestHelper, NullableTestHolder } from Todl::Compiler::Tests;\nimport { DateTime } from System;\n";

    public static IEnumerable<object[]> Cases()
    {
        // declarations, wrapping and null
        yield return new object[] { "int Run() { const int? a = 1; return a.Value; }", 1 };
        yield return new object[] { "bool Run() { const int? a = 1; return a.HasValue; }", true };
        yield return new object[] { "bool Run() { const int? a = null; return a.HasValue; }", false };
        yield return new object[] { "const int? G = 5; int Run() { return G.Value; }", 5 };
        yield return new object[] { "const int? G = null; bool Run() { return G.HasValue; }", false };
        yield return new object[] { "const int? G = 5; const int? H = G; int Run() { return H.GetValueOrDefault(9); }", 5 };
        yield return new object[] { "const int? G = 5; int? Get() { return G; } int Run() { return Get().Value; }", 5 };
        yield return new object[] { "long Run() { let long? c = 0L; return c.Value; }", 0L };
        yield return new object[] { "bool Run() { const bool? f = true; return f.Value; }", true };
        yield return new object[] { "bool Run() { const bool? f = false; return f.HasValue; }", true };
        yield return new object[] { "double Run() { const double? d = 2.5; return d.Value; }", 2.5 };
        yield return new object[] { "char Run() { const char? c = \"x\"[0]; return c.Value; }", 'x' };
        yield return new object[] { "int Run() { const System::Int32? a = 3; return a.Value; }", 3 };

        // GetValueOrDefault on empty and non-empty values
        yield return new object[] { "int Run() { const int? a = null; return a.GetValueOrDefault(); }", 0 };
        yield return new object[] { "int Run() { const int? a = 5; return a.GetValueOrDefault(); }", 5 };
        yield return new object[] { "int Run() { const int? a = null; return a.GetValueOrDefault(9); }", 9 };
        yield return new object[] { "int Run() { const int? a = 5; return a.GetValueOrDefault(9); }", 5 };
        yield return new object[] { "long Run() { const long? a = null; return a.GetValueOrDefault(7L); }", 7L };

        // assignments
        yield return new object[] { "int Run() { let int? x = null; x = 3; return x.Value; }", 3 };
        yield return new object[] { "bool Run() { let int? x = 3; x = null; return x.HasValue; }", false };
        yield return new object[] { "int Run() { let int? x = 3; let int? y = null; y = x; return y.Value; }", 3 };
        yield return new object[] { "int Run() { let int? x = null; return x.GetValueOrDefault(-1); }", -1 };

        // parameters and return values
        yield return new object[] { "int? Id(int? v) { return v; } int Run() { return Id(7).GetValueOrDefault(); }", 7 };
        yield return new object[] { "int? Id(int? v) { return v; } bool Run() { return Id(null).HasValue; }", false };
        yield return new object[] { "int? Id(int? v) { return v; } int Run() { const int? a = 4; return Id(a).Value; }", 4 };
        yield return new object[] { "int? Make(bool b) { if b { return 5; } return null; } int Run() { return Make(true).GetValueOrDefault(-1) * 10 + Make(false).GetValueOrDefault(1); }", 51 };
        yield return new object[] { "int Unwrap(int? v) { return v.GetValueOrDefault(100); } int Run() { return Unwrap(1) + Unwrap(null); }", 101 };
        yield return new object[] { "int Unwrap(int? v) { v = null; return v.GetValueOrDefault(3); } int Run() { return Unwrap(1); }", 3 };
        yield return new object[] { "int? Make(long? a) { return null; } bool Run() { return Make(null).HasValue; }", false };

        // function overloads
        yield return new object[] { "string F(int a) { return \"plain\"; } string F(int? a) { return \"nullable\"; } string Run() { return F(1); }", "plain" };
        yield return new object[] { "string F(int a) { return \"plain\"; } string F(int? a) { return \"nullable\"; } string Run() { return F(null); }", "nullable" };
        yield return new object[] { "string F(int a) { return \"plain\"; } string F(int? a) { return \"nullable\"; } string Run() { const int? n = 1; return F(n); }", "nullable" };
        yield return new object[] { "string F(int? a, int? b) { return \"x\"; } string Run() { return F(a: null, b: 2); }", "x" };
        yield return new object[] { "string F(int? a) { return \"x\"; } string Run() { return F(1); }", "x" };

        // arrays
        yield return new object[] { "int Run() { let int?[] xs = new int?[3]; xs[0] = 4; xs[1] = null; return xs[0].GetValueOrDefault() + xs[1].GetValueOrDefault(10) + xs[2].GetValueOrDefault(100); }", 114 };
        yield return new object[] { "bool Run() { const int?[] xs = new int?[2]; return xs[0].HasValue; }", false };
        yield return new object[] { "int Run() { let int?[][] xs = new int?[2][]; xs[0] = new int?[1]; xs[0][0] = 6; return xs[0][0].Value; }", 6 };
        yield return new object[] { "int Run() { let int?[] xs = new int?[1]; xs[0] = 2; return xs[0].Value; }", 2 };

        // CLR interop
        yield return new object[] { Imports + "int Run() { return NullableTestHelper.Echo(5).GetValueOrDefault(); }", 5 };
        yield return new object[] { Imports + "bool Run() { return NullableTestHelper.Echo(null).HasValue; }", false };
        yield return new object[] { Imports + "int Run() { const int? m = NullableTestHelper.Make(true, 8); return m.Value; }", 8 };
        yield return new object[] { Imports + "bool Run() { const int? m = NullableTestHelper.Make(false, 8); return m.HasValue; }", false };
        yield return new object[] { Imports + "string Run() { return NullableTestHelper.Describe(3); }", "value:3" };
        yield return new object[] { Imports + "string Run() { return NullableTestHelper.Describe(null); }", "empty" };
        yield return new object[] { Imports + "string Run() { const int? n = 9; return NullableTestHelper.Describe(n); }", "value:9" };
        yield return new object[] { Imports + "string Run() { return NullableTestHelper.Pick(1); }", "plain" };
        yield return new object[] { Imports + "string Run() { return NullableTestHelper.Pick(null); }", "nullable" };
        yield return new object[] { Imports + "string Run() { const int? n = 1; return NullableTestHelper.Pick(n); }", "nullable" };
        yield return new object[] { Imports + "string Run() { return NullableTestHelper.Only(null); }", "long?" };
        yield return new object[] { Imports + "string Run() { return NullableTestHelper.Only(2L); }", "long?" };
        yield return new object[] { Imports + "string Run() { return NullableTestHelper.Both(1); }", "int?" };
        yield return new object[] { Imports + "string Run() { return NullableTestHelper.Both(1L); }", "long?" };
        yield return new object[] { Imports + "string Run() { return NullableTestHelper.Named(first: 1, second: null); }", "1-" };
        yield return new object[] { Imports + "string Run() { return NullableTestHelper.Named(second: true, first: null); }", "-True" };
        yield return new object[] { Imports + "string Run() { return NullableTestHelper.Named(1, false); }", "1-False" };

        // CLR fields, properties and constructors
        yield return new object[] { Imports + "int Run() { let h = new NullableTestHolder(); h.Field = 3; return h.Field.Value; }", 3 };
        yield return new object[] { Imports + "bool Run() { let h = new NullableTestHolder(); h.Field = 3; h.Field = null; return h.Field.HasValue; }", false };
        yield return new object[] { Imports + "double Run() { let h = new NullableTestHolder(); h.Property = 2.5; return h.Property.Value; }", 2.5 };
        yield return new object[] { Imports + "bool Run() { let h = new NullableTestHolder(); h.Property = 2.5; h.Property = null; return h.Property.HasValue; }", false };
        yield return new object[] { Imports + "int Run() { const h = new NullableTestHolder(5); return h.Field.Value; }", 5 };
        yield return new object[] { Imports + "bool Run() { const h = new NullableTestHolder(null); return h.Field.HasValue; }", false };
        yield return new object[] { Imports + "int Run() { const h = new NullableTestHolder(field: 6); return h.Field.Value; }", 6 };
        yield return new object[] { Imports + "bool Run() { const h = new NullableTestHolder(field: null); return h.Field.HasValue; }", false };

        // other nullable value types
        yield return new object[] { Imports + "int Run() { const DateTime? d = new DateTime(2020, 1, 2); return d.Value.Year; }", 2020 };
        yield return new object[] { Imports + "bool Run() { const DateTime? d = null; return d.HasValue; }", false };
        yield return new object[] { Imports + "bool Run() { const System::DateTime? d = null; return d.HasValue; }", false };
        yield return new object[] { Imports + "int Run() { let System::DateTime? d = null; d = new DateTime(1999, 5, 6); return d.Value.Month; }", 5 };

        // nullable values are normal values in control flow
        yield return new object[] { "int Run() { let int? x = null; let total = 0; let i = 0; while i < 3 { x = i; total += x.Value; i += 1; } return total; }", 3 };
        yield return new object[] { "int Run() { const int? x = 2; if x.HasValue { return x.Value; } return -1; }", 2 };
        yield return new object[] { "int Run() { const int? x = null; unless x.HasValue { return -1; } return x.Value; }", -1 };

    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void NullableValueTypesShouldProduceExpectedResult(string source, object expected)
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

    [Fact]
    public void NullableParametersAndReturnsHaveNullableClrSignatures()
    {
        var (assemblyDefinition, _) = TestUtils.Compile(SourceText.FromString(
            "int? Echo(int? a) { return a; }\nint?[] Many(long? a) { return new int?[1]; }\nvoid Main() {}"));

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var type = assembly.EntryPoint.DeclaringType;
            var echo = type.GetMethod("Echo");

            echo.ReturnType.Should().Be(typeof(int?));
            echo.GetParameters()[0].ParameterType.Should().Be(typeof(int?));
            echo.Invoke(null, [null]).Should().BeNull();
            echo.Invoke(null, [3]).Should().Be(3);

            type.GetMethod("Many").ReturnType.Should().Be(typeof(int?[]));
            type.GetMethod("Many").GetParameters()[0].ParameterType.Should().Be(typeof(long?));
        });
    }
}
