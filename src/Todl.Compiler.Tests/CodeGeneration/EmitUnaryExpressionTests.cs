using Mono.Cecil.Cil;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitUnaryExpressionTests
{
    [Fact]
    public void TestEmitUnaryPlusExpressionLocalVariable()
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
    public void TestEmitUnaryMinusExpressionLocalVariable()
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
    public void TestEmitLogicalNegationExpressionLocalVariable()
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
    public void TestEmitBitwiseComplementExpressionLocalVariable()
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
    public void TestEmitUnaryExpressionWithInstanceField()
    {
        TestUtils.EmitExpressionAndVerify(
            "+Todl.Compiler.Tests.TestClass.Instance.PublicIntField",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldfld, "System.Int32 PublicIntField"));

        TestUtils.EmitExpressionAndVerify(
            "-Todl.Compiler.Tests.TestClass.Instance.PublicIntField",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldfld, "System.Int32 PublicIntField"),
            TestInstruction.Create(OpCodes.Neg));

        TestUtils.EmitExpressionAndVerify(
            "!Todl.Compiler.Tests.TestClass.Instance.PublicBoolField",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldfld, "System.Boolean PublicBoolField"),
            TestInstruction.Create(OpCodes.Ldc_I4_0),
            TestInstruction.Create(OpCodes.Ceq));

        TestUtils.EmitExpressionAndVerify(
            "~Todl.Compiler.Tests.TestClass.Instance.PublicIntField",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldfld, "System.Int32 PublicIntField"),
            TestInstruction.Create(OpCodes.Not));
    }

    [Fact]
    public void TestEmitUnaryExpressionWithStaticField()
    {
        TestUtils.EmitExpressionAndVerify(
            "+Todl.Compiler.Tests.TestClass.PublicStaticIntField",
            TestInstruction.Create(OpCodes.Ldsfld, "System.Int32 PublicStaticIntField"));

        TestUtils.EmitExpressionAndVerify(
            "-Todl.Compiler.Tests.TestClass.PublicStaticIntField",
            TestInstruction.Create(OpCodes.Ldsfld, "System.Int32 PublicStaticIntField"),
            TestInstruction.Create(OpCodes.Neg));

        TestUtils.EmitExpressionAndVerify(
            "!Todl.Compiler.Tests.TestClass.PublicStaticBoolField",
            TestInstruction.Create(OpCodes.Ldsfld, "System.Boolean PublicStaticBoolField"),
            TestInstruction.Create(OpCodes.Ldc_I4_0),
            TestInstruction.Create(OpCodes.Ceq));

        TestUtils.EmitExpressionAndVerify(
            "~Todl.Compiler.Tests.TestClass.PublicStaticIntField",
            TestInstruction.Create(OpCodes.Ldsfld, "System.Int32 PublicStaticIntField"),
            TestInstruction.Create(OpCodes.Not));
    }

    [Fact]
    public void TestEmitUnaryExpressionWithInstanceProperty()
    {
        TestUtils.EmitExpressionAndVerify(
            "+Todl.Compiler.Tests.TestClass.Instance.PublicIntProperty",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Callvirt, "System.Int32 Todl.Compiler.Tests.TestClass::get_PublicIntProperty()"));

        TestUtils.EmitExpressionAndVerify(
            "-Todl.Compiler.Tests.TestClass.Instance.PublicIntProperty",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Callvirt, "System.Int32 Todl.Compiler.Tests.TestClass::get_PublicIntProperty()"),
            TestInstruction.Create(OpCodes.Neg));

        TestUtils.EmitExpressionAndVerify(
            "!Todl.Compiler.Tests.TestClass.Instance.PublicBoolProperty",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Callvirt, "System.Boolean Todl.Compiler.Tests.TestClass::get_PublicBoolProperty()"),
            TestInstruction.Create(OpCodes.Ldc_I4_0),
            TestInstruction.Create(OpCodes.Ceq));

        TestUtils.EmitExpressionAndVerify(
            "~Todl.Compiler.Tests.TestClass.Instance.PublicIntProperty",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Callvirt, "System.Int32 Todl.Compiler.Tests.TestClass::get_PublicIntProperty()"),
            TestInstruction.Create(OpCodes.Not));
    }

    [Fact]
    public void TestEmitUnaryExpressionWithStaticProperty()
    {
        TestUtils.EmitExpressionAndVerify(
            "+Todl.Compiler.Tests.TestClass.PublicStaticIntProperty",
            TestInstruction.Create(OpCodes.Call, "System.Int32 Todl.Compiler.Tests.TestClass::get_PublicStaticIntProperty()"));

        TestUtils.EmitExpressionAndVerify(
            "-Todl.Compiler.Tests.TestClass.PublicStaticIntProperty",
            TestInstruction.Create(OpCodes.Call, "System.Int32 Todl.Compiler.Tests.TestClass::get_PublicStaticIntProperty()"),
            TestInstruction.Create(OpCodes.Neg));

        TestUtils.EmitExpressionAndVerify(
            "!Todl.Compiler.Tests.TestClass.PublicStaticBoolProperty",
            TestInstruction.Create(OpCodes.Call, "System.Boolean Todl.Compiler.Tests.TestClass::get_PublicStaticBoolProperty()"),
            TestInstruction.Create(OpCodes.Ldc_I4_0),
            TestInstruction.Create(OpCodes.Ceq));

        TestUtils.EmitExpressionAndVerify(
            "~Todl.Compiler.Tests.TestClass.PublicStaticIntProperty",
            TestInstruction.Create(OpCodes.Call, "System.Int32 Todl.Compiler.Tests.TestClass::get_PublicStaticIntProperty()"),
            TestInstruction.Create(OpCodes.Not));
    }
}
