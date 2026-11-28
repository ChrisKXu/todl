using Mono.Cecil.Cil;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitNullableConversionTests
{
    [Fact]
    public void TestEmitValueToNullableConversion()
    {
        TestUtils.EmitStatementAndVerify(
            "const int? a = 1;",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Newobj, "System.Void System.Nullable`1<System.Int32>::.ctor(T)"),
            TestInstruction.Create(OpCodes.Stloc_0));
    }

    [Fact]
    public void TestEmitNullToNullableConversion()
    {
        TestUtils.EmitStatementAndVerify(
            "const int? a = null;",
            TestInstruction.Create(OpCodes.Ldloca_S, 1),
            TestInstruction.Create(OpCodes.Initobj, "System.Nullable`1<System.Int32>"),
            TestInstruction.Create(OpCodes.Ldloc_1),
            TestInstruction.Create(OpCodes.Stloc_0));
    }

    [Fact]
    public void TestEmitNullableAssignment()
    {
        TestUtils.EmitStatementAndVerify(
            "{ let long? a = 1L; a = 2L; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Newobj, "System.Void System.Nullable`1<System.Int64>::.ctor(T)"),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldc_I4_2),
            TestInstruction.Create(OpCodes.Conv_I8),
            TestInstruction.Create(OpCodes.Newobj, "System.Void System.Nullable`1<System.Int64>::.ctor(T)"),
            TestInstruction.Create(OpCodes.Stloc_0));
    }

    [Fact]
    public void TestEmitNullableMemberAccessUsesTheAddressOfTheReceiver()
    {
        TestUtils.EmitStatementAndVerify(
            "{ const int? a = 1; a.HasValue; }",
            TestInstruction.Create(OpCodes.Ldc_I4_1),
            TestInstruction.Create(OpCodes.Newobj, "System.Void System.Nullable`1<System.Int32>::.ctor(T)"),
            TestInstruction.Create(OpCodes.Stloc_0),
            TestInstruction.Create(OpCodes.Ldloca_S, 0),
            TestInstruction.Create(OpCodes.Call, "System.Boolean System.Nullable`1<System.Int32>::get_HasValue()"));
    }
}
