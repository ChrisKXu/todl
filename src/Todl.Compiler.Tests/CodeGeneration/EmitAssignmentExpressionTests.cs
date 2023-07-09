using System.Net.Http.Headers;
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
    public void TestEmitAssignmentExpressionWithClassFields()
    {
        TestEmitAssignmentExpressionCore(
            "{ Todl.Compiler.Tests.TestClass.PublicStaticIntField = 10; }",
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Stsfld, (typeof(int).FullName, nameof(TestClass.PublicStaticIntField))));

        TestEmitAssignmentExpressionCore(
            "{ Todl.Compiler.Tests.TestClass.PublicStaticStringField = \"abc\"; }",
            TestInstruction.Create(OpCodes.Ldstr, "abc"),
            TestInstruction.Create(OpCodes.Stsfld, (typeof(string).FullName, nameof(TestClass.PublicStaticStringField))));

        TestEmitAssignmentExpressionCore(
            "{ Todl.Compiler.Tests.TestClass.Instance.PublicIntField = 10; }",
            TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)10),
            TestInstruction.Create(OpCodes.Stfld, (typeof(int).FullName, nameof(TestClass.PublicIntField))));

        TestEmitAssignmentExpressionCore(
            "{ Todl.Compiler.Tests.TestClass.Instance.PublicStringField = \"abc\"; }",
            TestInstruction.Create(OpCodes.Ldstr, "abc"),
            TestInstruction.Create(OpCodes.Stfld, (typeof(string).FullName, nameof(TestClass.PublicStringField))));
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
