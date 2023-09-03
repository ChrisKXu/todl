using FluentAssertions;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitAssignmentExpressionTests
{
    [Fact]
    public void TestEmitAssignmentExpressionWithLocalVariables()
    {
        TestUtils.EmitStatementAndVerify(
            "{ let a = 1; a = 10; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Stloc_0));

        TestUtils.EmitStatementAndVerify(
            "{ let a = 1; a += 10; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Stloc_0));

        TestUtils.EmitStatementAndVerify(
            "{ let a = 1; a -= 10; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Stloc_0));

        TestUtils.EmitStatementAndVerify(
            "{ let a = 1; a *= 10; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Mul),
            TestInstruction.Create(OpCodes.Stloc_0));

        TestUtils.EmitStatementAndVerify(
            "{ let a = 1; a /= 10; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Div),
            TestInstruction.Create(OpCodes.Stloc_0));
    }

    [Fact]
    public void TestEmitAssignmentExpressionWithLocalVariablesWithSideEffectPrefixIncrement()
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
    public void TestEmitAssignmentExpressionWithLocalVariablesWithSideEffectPrefixDecrement()
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
    public void TestEmitAssignmentExpressionWithLocalVariablesWithSideEffectPostfixIncrement()
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
    public void TestEmitAssignmentExpressionWithLocalVariablesWithSideEffectPostfixDecrement()
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

    [Fact]
    public void TestEmitAssignmentExpressionWithStaticFields()
    {
        TestUtils.EmitStatementAndVerify(
            "Todl.Compiler.Tests.TestClass.PublicStaticIntField = 10;",
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Stsfld, "System.Int32 PublicStaticIntField"));

        TestUtils.EmitStatementAndVerify(
            "Todl.Compiler.Tests.TestClass.PublicStaticStringField = \"abc\";",
            TestInstruction.Create(OpCodes.Ldstr, "abc"),
            TestInstruction.Create(OpCodes.Stsfld, "System.String PublicStaticStringField"));
    }

    [Fact]
    public void TestEmitAssignmentExpressionWithInstanceFields()
    {
        TestUtils.EmitStatementAndVerify(
            "Todl.Compiler.Tests.TestClass.Instance.PublicIntField = 10;",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Stfld, "System.Int32 PublicIntField"));

        TestUtils.EmitStatementAndVerify(
            "Todl.Compiler.Tests.TestClass.Instance.PublicStringField = \"abc\";",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldstr, "abc"),
            TestInstruction.Create(OpCodes.Stfld, "System.String PublicStringField"));
    }

    [Fact]
    public void TestEmitAssignmentExpressionWithStaticProperties()
    {
        TestUtils.EmitStatementAndVerify(
            "Todl.Compiler.Tests.TestClass.PublicStaticIntProperty = 10;",
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Call, "System.Void Todl.Compiler.Tests.TestClass::set_PublicStaticIntProperty(System.Int32)"));

        TestUtils.EmitStatementAndVerify(
            "Todl.Compiler.Tests.TestClass.PublicStaticStringProperty = \"abc\";",
            TestInstruction.Create(OpCodes.Ldstr, "abc"),
            TestInstruction.Create(OpCodes.Call, "System.Void Todl.Compiler.Tests.TestClass::set_PublicStaticStringProperty(System.String)"));
    }

    [Fact]
    public void TestEmitAssignmentExpressionWithInstanceProperties()
    {
        TestUtils.EmitStatementAndVerify(
            "Todl.Compiler.Tests.TestClass.Instance.PublicIntProperty = 10;",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Callvirt, "System.Void Todl.Compiler.Tests.TestClass::set_PublicIntProperty(System.Int32)"));

        TestUtils.EmitStatementAndVerify(
            "Todl.Compiler.Tests.TestClass.Instance.PublicStringProperty = \"abc\";",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldstr, "abc"),
            TestInstruction.Create(OpCodes.Callvirt, "System.Void Todl.Compiler.Tests.TestClass::set_PublicStringProperty(System.String)"));
    }

    [Fact]
    public void TestEmitAssignmentExpressionWithSelfAssignmentInstanceFields()
    {
        TestUtils.EmitStatementAndVerify(
            "Todl.Compiler.Tests.TestClass.Instance.PublicIntField = Todl.Compiler.Tests.TestClass.Instance.PublicIntField;",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldfld, "System.Int32 PublicIntField"),
            TestInstruction.Create(OpCodes.Stfld, "System.Int32 PublicIntField"));

        TestUtils.EmitStatementAndVerify(
            "Todl.Compiler.Tests.TestClass.Instance.PublicIntField = Todl.Compiler.Tests.TestClass.Instance.PublicIntField + 1;",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldfld, "System.Int32 PublicIntField"),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Stfld, "System.Int32 PublicIntField"));
    }

    [Fact]
    public void TestEmitAssignmentExpressionWithSelfAssignmentInstanceProperties()
    {
        TestUtils.EmitStatementAndVerify(
            "Todl.Compiler.Tests.TestClass.Instance.PublicIntProperty = Todl.Compiler.Tests.TestClass.Instance.PublicIntProperty;",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Callvirt, "System.Int32 Todl.Compiler.Tests.TestClass::get_PublicIntProperty()"),
            TestInstruction.Create(OpCodes.Callvirt, "System.Void Todl.Compiler.Tests.TestClass::set_PublicIntProperty(System.Int32)"));

        TestUtils.EmitStatementAndVerify(
            "Todl.Compiler.Tests.TestClass.Instance.PublicIntProperty = Todl.Compiler.Tests.TestClass.Instance.PublicIntProperty + 1;",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Callvirt, "System.Int32 Todl.Compiler.Tests.TestClass::get_PublicIntProperty()"),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Callvirt, "System.Void Todl.Compiler.Tests.TestClass::set_PublicIntProperty(System.Int32)"));
    }
}
