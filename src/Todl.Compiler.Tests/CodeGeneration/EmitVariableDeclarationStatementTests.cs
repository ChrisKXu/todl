using Mono.Cecil.Cil;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitVariableDeclarationStatementTests
{
    [Fact]
    public void TestEmitVariableDeclarationStatement()
    {
        TestUtils.EmitStatementAndVerify(
            "{ const a = 0; const b = 1; const c = 2; let d = 3; let e = 4; let f = 5; }",
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
