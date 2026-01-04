using Mono.Cecil.Cil;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitLoopStatementTests
{
    [Fact]
    public void BoundLoopStatementsCanHaveEmptyBody()
    {
        TestUtils.EmitStatementAndVerify(
            "while true { }",
            TestInstruction.Create(OpCodes.Br_S, 3),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Brtrue_S, 2));

        TestUtils.EmitStatementAndVerify(
            "while false { }",
            TestInstruction.Create(OpCodes.Br_S, 3),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Ldc_I4_0),
            TestInstruction.Create(OpCodes.Brtrue_S, 2));

        TestUtils.EmitStatementAndVerify(
            "until true { }",
            TestInstruction.Create(OpCodes.Br_S, 3),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Brfalse_S, 2));

        TestUtils.EmitStatementAndVerify(
            "until false { }",
            TestInstruction.Create(OpCodes.Br_S, 3),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Ldc_I4_0),
            TestInstruction.Create(OpCodes.Brfalse_S, 2));
    }

    [Fact]
    public void BoundLoopStatementsCanHaveOneOrMoreInnerStatements()
    {
        TestUtils.EmitStatementAndVerify(
            "while Todl::Compiler::Tests::TestClass.PublicStaticIntField != 0 { System::Console.WriteLine(); }",
            TestInstruction.Create(OpCodes.Br_S, 8),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Call, "System.Void System.Console::WriteLine()"),
            TestInstruction.Create(OpCodes.Nop),
            TestInstruction.Create(OpCodes.Ldsfld, "System.Int32 PublicStaticIntField"),
            TestInstruction.Create(OpCodes.Ldc_I4_0),
            TestInstruction.Create(OpCodes.Ceq),
            TestInstruction.Create(OpCodes.Ldc_I4_0),
            TestInstruction.Create(OpCodes.Ceq),
            TestInstruction.Create(OpCodes.Brtrue_S, 2));
    }
}
