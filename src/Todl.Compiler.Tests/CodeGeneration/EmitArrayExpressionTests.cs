using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitArrayExpressionTests
{
    [Fact]
    public void TestEmitArrayCreation()
    {
        TestUtils.EmitExpressionAndVerify(
            "new int[5]",
            TestInstruction.Create(OpCodes.Ldc_I4_5),
            TestInstruction.Create(OpCodes.Newarr, "System.Int32"));

        TestUtils.EmitExpressionAndVerify(
            "new int[3][]",
            TestInstruction.Create(OpCodes.Ldc_I4_3),
            TestInstruction.Create(OpCodes.Newarr, "System.Int32[]"));
    }

    [Fact]
    public void TestEmitArrayLengthUsesLdlen()
    {
        TestUtils.EmitStatementAndVerify(
            "{ let a = new int[2]; let n = a.Length; }",
            TestInstruction.Create(OpCodes.Ldc_I4_2),
            TestInstruction.Create(OpCodes.Newarr, "System.Int32"),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldlen),
            TestInstruction.Create(OpCodes.Conv_I4),
            TestInstruction.Create(OpCodes.Stloc_1));
    }

    [Fact]
    public void TestEmitArrayLiteral()
    {
        TestUtils.EmitExpressionAndVerify(
            "[1, 2]",
            TestInstruction.Create(OpCodes.Ldc_I4_2),
            TestInstruction.Create(OpCodes.Newarr, "System.Int32"),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_I4_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stelem_I4),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Ldc_I4_2),
            TestInstruction.Create(OpCodes.Stelem_I4));
    }

    [Fact]
    public void TestEmitNestedArrayLiteral()
    {
        TestUtils.EmitExpressionAndVerify(
            "[[1]]",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Newarr, "System.Int32[]"),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_I4_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Newarr, "System.Int32"),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_I4_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stelem_I4),
            TestInstruction.Create(OpCodes.Stelem_Ref));
    }

    [Fact]
    public void TestEmitArrayElementLoad()
    {
        TestUtils.EmitStatementAndVerify(
            "{ let a = new int[2]; let b = a[1]; }",
            TestInstruction.Create(OpCodes.Ldc_I4_2),
            TestInstruction.Create(OpCodes.Newarr, "System.Int32"),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Ldelem_I4),
            TestInstruction.Create(OpCodes.Stloc_1));
    }

    [Fact]
    public void TestEmitArrayElementStore()
    {
        TestUtils.EmitStatementAndVerify(
            "{ let a = new int[2]; a[1] = 7; }",
            TestInstruction.Create(OpCodes.Ldc_I4_2),
            TestInstruction.Create(OpCodes.Newarr, "System.Int32"),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Ldc_I4_7),
            TestInstruction.Create(OpCodes.Stelem_I4));
    }

    [Fact]
    public void TestEmitArrayElementCompoundAssignmentEvaluatesBaseAndIndexOnce()
    {
        TestUtils.EmitStatementAndVerify(
            "{ let a = new int[2]; a[1] += 3; }",
            TestInstruction.Create(OpCodes.Ldc_I4_2),
            TestInstruction.Create(OpCodes.Newarr, "System.Int32"),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Stloc_1),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_2),
            TestInstruction.Create(OpCodes.Ldloc_1),
            TestInstruction.Create(OpCodes.Ldloc_2),
            TestInstruction.Create(OpCodes.Ldloc_1),
            TestInstruction.Create(OpCodes.Ldloc_2),
            TestInstruction.Create(OpCodes.Ldelem_I4),
            TestInstruction.Create(OpCodes.Ldc_I4_3),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Stelem_I4));
    }

    public static IEnumerable<object[]> ElementOpCodes()
    {
        // new-expression, literal, newarr operand, ldelem, stelem
        yield return new object[] { "new int[1]", "1", "System.Int32", OpCodes.Ldelem_I4, OpCodes.Stelem_I4 };
        yield return new object[] { "new long[1]", "1L", "System.Int64", OpCodes.Ldelem_I8, OpCodes.Stelem_I8 };
        yield return new object[] { "new float[1]", "1.0f", "System.Single", OpCodes.Ldelem_R4, OpCodes.Stelem_R4 };
        yield return new object[] { "new double[1]", "1.0", "System.Double", OpCodes.Ldelem_R8, OpCodes.Stelem_R8 };
        yield return new object[] { "new bool[1]", "true", "System.Boolean", OpCodes.Ldelem_U1, OpCodes.Stelem_I1 };
        yield return new object[] { "new string[1]", "\"s\"", "System.String", OpCodes.Ldelem_Ref, OpCodes.Stelem_Ref };
        yield return new object[] { "new int[1][]", "[1]", "System.Int32[]", OpCodes.Ldelem_Ref, OpCodes.Stelem_Ref };
    }

    [Theory]
    [MemberData(nameof(ElementOpCodes))]
    public void TestEmitElementOpCodeMatchesElementType(
        string newExpression, string literal, string elementType, OpCode ldelem, OpCode stelem)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var statement = TestUtils.BindStatement<BoundStatement>(
            $"{{ let a = {newExpression}; let b = a[0]; a[0] = {literal}; }}", diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();

        var emitter = new TestEmitter();
        emitter.EmitStatement(statement);
        emitter.Emit();

        var instructions = emitter.ILProcessor.Body.Instructions.Select(TestInstruction.FromInstruction).ToList();
        instructions[0..2].Should().Equal(
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Newarr, elementType));
        instructions[5].Should().Be(TestInstruction.Create(ldelem));
        instructions[^1].Should().Be(TestInstruction.Create(stelem));
    }

    [Fact]
    public void TestEmitClrIndexerLoad()
    {
        TestUtils.EmitStatementAndVerify(
            "{ let s = \"abc\"; let c = s[1]; }",
            TestInstruction.Create(OpCodes.Ldstr, "abc"),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Callvirt, "System.Char System.String::get_Chars(System.Int32)"),
            TestInstruction.Create(OpCodes.Stloc_1));
    }
}
