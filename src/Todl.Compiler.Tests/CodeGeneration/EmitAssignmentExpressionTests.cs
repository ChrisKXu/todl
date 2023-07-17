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
        TestEmitAssignmentExpressionCore(
            "{ let a = 1; a = 10; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Stloc_0));

        TestEmitAssignmentExpressionCore(
            "{ let a = 1; a += 10; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Add),
            TestInstruction.Create(OpCodes.Stloc_0));

        TestEmitAssignmentExpressionCore(
            "{ let a = 1; a -= 10; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Sub),
            TestInstruction.Create(OpCodes.Stloc_0));

        TestEmitAssignmentExpressionCore(
            "{ let a = 1; a *= 10; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Mul),
            TestInstruction.Create(OpCodes.Stloc_0));

        TestEmitAssignmentExpressionCore(
            "{ let a = 1; a /= 10; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Div),
            TestInstruction.Create(OpCodes.Stloc_0));
    }

    [Fact]
    public void TestEmitAssignmentExpressionWithStaticFields()
    {
        TestEmitAssignmentExpressionCore(
            "{ Todl.Compiler.Tests.TestClass.PublicStaticIntField = 10; }",
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Stsfld, "System.Int32 PublicStaticIntField"));

        TestEmitAssignmentExpressionCore(
            "{ Todl.Compiler.Tests.TestClass.PublicStaticStringField = \"abc\"; }",
            TestInstruction.Create(OpCodes.Ldstr, "abc"),
            TestInstruction.Create(OpCodes.Stsfld, "System.String PublicStaticStringField"));
    }

    [Fact]
    public void TestEmitAssignmentExpressionWithInstanceFields()
    {
        TestEmitAssignmentExpressionCore(
            "{ Todl.Compiler.Tests.TestClass.Instance.PublicIntField = 10; }",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Stfld, "System.Int32 PublicIntField"));

        TestEmitAssignmentExpressionCore(
            "{ Todl.Compiler.Tests.TestClass.Instance.PublicStringField = \"abc\"; }",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldstr, "abc"),
            TestInstruction.Create(OpCodes.Stfld, "System.String PublicStringField"));
    }

    [Fact]
    public void TestEmitAssignmentExpressionWithStaticProperties()
    {
        TestEmitAssignmentExpressionCore(
            "{ Todl.Compiler.Tests.TestClass.PublicStaticIntProperty = 10; }",
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Call, "System.Void Todl.Compiler.Tests.TestClass::set_PublicStaticIntProperty(System.Int32)"));

        TestEmitAssignmentExpressionCore(
            "{ Todl.Compiler.Tests.TestClass.PublicStaticStringProperty = \"abc\"; }",
            TestInstruction.Create(OpCodes.Ldstr, "abc"),
            TestInstruction.Create(OpCodes.Call, "System.Void Todl.Compiler.Tests.TestClass::set_PublicStaticStringProperty(System.String)"));
    }

    [Fact]
    public void TestEmitAssignmentExpressionWithInstanceProperties()
    {
        TestEmitAssignmentExpressionCore(
            "{ Todl.Compiler.Tests.TestClass.Instance.PublicIntProperty = 10; }",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Callvirt, "System.Void Todl.Compiler.Tests.TestClass::set_PublicIntProperty(System.Int32)"));

        TestEmitAssignmentExpressionCore(
            "{ Todl.Compiler.Tests.TestClass.Instance.PublicStringProperty = \"abc\"; }",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldstr, "abc"),
            TestInstruction.Create(OpCodes.Callvirt, "System.Void Todl.Compiler.Tests.TestClass::set_PublicStringProperty(System.String)"));
    }

    private void TestEmitAssignmentExpressionCore(string input, params TestInstruction[] expectedInstructions)
    {
        var boundBlockStatement = TestUtils.BindStatement<BoundBlockStatement>(input);
        boundBlockStatement.GetDiagnostics().Should().BeEmpty();

        var emitter = new TestEmitter();
        emitter.EmitStatement(boundBlockStatement);

        emitter.ILProcessor.Body.Instructions.ShouldHaveExactInstructionSequence(expectedInstructions);
    }
}
