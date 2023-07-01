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

    private void TestEmitAssignmentExpressionCore(string input, params TestInstruction[] expectedInstructions)
    {
        var boundBlockStatement = TestUtils.BindStatement<BoundBlockStatement>(input);
        var emitter = new TestEmitter();
        emitter.EmitStatement(boundBlockStatement);

        emitter.ILProcessor.Body.Instructions.ShouldHaveExactInstructionSequence(expectedInstructions);
    }
}
