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
            (OpCodes.Ldc_I4_0, null),
            (OpCodes.Stloc_0, null),
            (OpCodes.Ldc_I4_1, null),
            (OpCodes.Stloc_1, null),
            (OpCodes.Ldc_I4_2, null),
            (OpCodes.Stloc_2, null),
            (OpCodes.Ldc_I4_3, null),
            (OpCodes.Stloc_3, null),
            (OpCodes.Ldc_I4_4, null),
            (OpCodes.Stloc_S, 4),
            (OpCodes.Ldc_I4_5, null),
            (OpCodes.Stloc_S, 5));
    }
}
