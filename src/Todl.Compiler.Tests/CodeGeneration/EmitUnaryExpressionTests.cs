using Mono.Cecil.Cil;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitUnaryExpressionTests
{
    [Fact]
    public void TestEmitUnaryPlusExpression()
    {
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1; let b = +a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1U; let b = +a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1L; let b = +a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1UL; let b = +a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1.0F; let b = +a; }",
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1.0; let b = +a; }",
            TestInstruction.Create(OpCodes.Ldc_R8, 1.0),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
    }

    [Fact]
    public void TestEmitUnaryMinusExpression()
    {
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1; let b = -a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Neg),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1U; let b = -a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Conv_U8),
            TestInstruction.Create(OpCodes.Neg),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1L; let b = -a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Neg),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1.0F; let b = -a; }",
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Neg),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1.0; let b = -a; }",
            TestInstruction.Create(OpCodes.Ldc_R8, 1.0),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Neg),
            TestInstruction.Create(OpCodes.Stloc_1));
    }

    [Fact]
    public void TestEmitLogicalNegationExpression()
    {
        TestUtils.EmitStatementAndVerify(
            "{ let a = true; let b = !a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_0),
            TestInstruction.Create(OpCodes.Ceq),
            TestInstruction.Create(OpCodes.Stloc_1));
    }

    [Fact]
    public void TestEmitBitwiseComplementExpression()
    {
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1; let b = ~a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Not),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1U; let b = ~a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Not),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1L; let b = ~a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Not),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1UL; let b = ~a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Not),
            TestInstruction.Create(OpCodes.Stloc_1));
    }

    [Fact]
    public void TestEmitPrefixIncrementExpression()
    {
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1; let b = ++a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1U; let b = ++a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1L; let b = ++a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1UL; let b = ++a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1.0F; let b = ++a; }",
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1.0; let b = ++a; }",
            TestInstruction.Create(OpCodes.Ldc_R8, 1.0),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_R8, 1.0),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
    }

    [Fact]
    public void TestEmitPrefixDecrementExpression()
    {
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1; let b = --a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1U; let b = --a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1L; let b = --a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1UL; let b = --a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1.0F; let b = --a; }",
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1.0; let b = --a; }",
            TestInstruction.Create(OpCodes.Ldc_R8, 1.0),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_R8, 1.0),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
    }

    [Fact]
    public void TestEmitPostfixIncrementExpression()
    {
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1; let b = a++; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1U; let b = a++; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1L; let b = a++; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1UL; let b = a++; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1.0F; let b = a++; }",
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1.0; let b = a++; }",
            TestInstruction.Create(OpCodes.Ldc_R8, 1.0),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_R8, 1.0),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
    }

    [Fact]
    public void TestEmitPostfixDecrementExpression()
    {
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1; let b = a--; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1U; let b = a--; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1L; let b = a--; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1UL; let b = a--; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1.0F; let b = a--; }",
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1.0; let b = a--; }",
            TestInstruction.Create(OpCodes.Ldc_R8, 1.0),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_R8, 1.0),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
    }
}
