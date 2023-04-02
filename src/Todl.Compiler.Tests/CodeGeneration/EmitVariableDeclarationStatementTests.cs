using FluentAssertions;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitVariableDeclarationStatementTests
{
    [Fact]
    public void TestEmitVariableDeclarationStatement()
    {
        var input = "{ const a = 0; const b = 1; const c = 2; let d = 3; let e = 4; let f = 5; }";
        var boundBlockStatement = TestUtils.BindStatement<BoundBlockStatement>(input);

        var emitter = new TestEmitter();
        emitter.EmitStatement(boundBlockStatement);

        emitter.ILProcessor.Body.Instructions.ShouldHaveExactInstructionSequence(
            TestInstruction.Create(OpCodes.Ldc_I4_0),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Stloc_1),
            TestInstruction.Create(OpCodes.Ldc_I4_2),
            TestInstruction.Create(OpCodes.Stloc_2),
            TestInstruction.Create(OpCodes.Ldc_I4_3),
            TestInstruction.Create(OpCodes.Stloc_3),
            TestInstruction.Create(OpCodes.Ldc_I4_4),
            TestInstruction.Create(OpCodes.Stloc_S, 4),
            TestInstruction.Create(OpCodes.Ldc_I4_5),
            TestInstruction.Create(OpCodes.Stloc_S, 5));
    }
}
