using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitUnaryExpressionTests
{
    [Fact]
    public void TestEmitUnaryPlusExpression()
    {
        TestEmitUnaryExpressionCore(
            "{ let a = 1; let b = +a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
            "{ let a = 1U; let b = +a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
            "{ let a = 1L; let b = +a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
            "{ let a = 1UL; let b = +a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
            "{ let a = 1.0F; let b = +a; }",
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
            "{ let a = 1.0; let b = +a; }",
            TestInstruction.Create(OpCodes.Ldc_R8, 1.0),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
    }

    [Fact]
    public void TestEmitUnaryMinusExpression()
    {
        TestEmitUnaryExpressionCore(
            "{ let a = 1; let b = -a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Neg),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
            "{ let a = 1U; let b = -a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Conv_U8),
            TestInstruction.Create(OpCodes.Neg),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
            "{ let a = 1L; let b = -a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Neg),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
            "{ let a = 1.0F; let b = -a; }",
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Neg),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
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
        TestEmitUnaryExpressionCore(
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
        TestEmitUnaryExpressionCore(
            "{ let a = 1; let b = ~a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Not),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
            "{ let a = 1U; let b = ~a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Not),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
            "{ let a = 1L; let b = ~a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Not),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
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
        TestEmitUnaryExpressionCore(
            "{ let a = 1; let b = ++a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
            "{ let a = 1U; let b = ++a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
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
        TestEmitUnaryExpressionCore(
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
        TestEmitUnaryExpressionCore(
            "{ let a = 1.0F; let b = ++a; }",
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
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
        TestEmitUnaryExpressionCore(
            "{ let a = 1; let b = --a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
            "{ let a = 1U; let b = --a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
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
        TestEmitUnaryExpressionCore(
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
        TestEmitUnaryExpressionCore(
            "{ let a = 1.0F; let b = --a; }",
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
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
        TestEmitUnaryExpressionCore(
            "{ let a = 1; let b = a++; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
            "{ let a = 1U; let b = a++; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
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
        TestEmitUnaryExpressionCore(
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
        TestEmitUnaryExpressionCore(
            "{ let a = 1.0F; let b = a++; }",
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
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
        TestEmitUnaryExpressionCore(
            "{ let a = 1; let b = a--; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
            "{ let a = 1U; let b = a--; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
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
        TestEmitUnaryExpressionCore(
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
        TestEmitUnaryExpressionCore(
            "{ let a = 1.0F; let b = a--; }",
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
            TestInstruction.Create(OpCodes.Dup),
            TestInstruction.Create(OpCodes.Ldc_R4, 1F),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Stloc_1));
        TestEmitUnaryExpressionCore(
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

    private void TestEmitUnaryExpressionCore(string input, params TestInstruction[] expectedInstructions)
    {
        var boundBlockStatement = TestUtils.BindStatement<BoundBlockStatement>(input);
        var emitter = new TestEmitter();
        emitter.EmitStatement(boundBlockStatement);

        emitter.ILProcessor.Body.Instructions.ShouldHaveExactInstructionSequence(expectedInstructions);
    }
}
