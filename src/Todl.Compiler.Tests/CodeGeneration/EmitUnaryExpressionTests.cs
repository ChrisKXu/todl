using FluentAssertions;
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
            "{ let a = 5; let b = +a; }",
            TestInstruction.Create(OpCodes.Ldc_I4_5),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloc_0),
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
