using Mono.Cecil.Cil;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitMemberAccessExpressionTests
{
    [Fact]
    public void TestEmitClrFieldLoad()
    {
        TestUtils.EmitExpressionAndVerify(
            "Todl.Compiler.Tests.TestClass.PublicStaticIntField",
            TestInstruction.Create(OpCodes.Ldsfld, "System.Int32 PublicStaticIntField"));

        TestUtils.EmitExpressionAndVerify(
            "Todl.Compiler.Tests.TestClass.PublicStaticStringField",
            TestInstruction.Create(OpCodes.Ldsfld, "System.String PublicStaticStringField"));

        TestUtils.EmitExpressionAndVerify(
            "Todl.Compiler.Tests.TestClass.Instance.PublicIntField",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldfld, "System.Int32 PublicIntField"));

        TestUtils.EmitExpressionAndVerify(
            "Todl.Compiler.Tests.TestClass.Instance.PublicStringField",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Ldfld, "System.String PublicStringField"));
    }

    [Fact]
    public void TestEmitClrPropertyLoad()
    {
        TestUtils.EmitExpressionAndVerify(
            "Todl.Compiler.Tests.TestClass.PublicStaticIntProperty",
            TestInstruction.Create(OpCodes.Call, "System.Int32 Todl.Compiler.Tests.TestClass::get_PublicStaticIntProperty()"));

        TestUtils.EmitExpressionAndVerify(
            "Todl.Compiler.Tests.TestClass.PublicStaticStringProperty",
            TestInstruction.Create(OpCodes.Call, "System.String Todl.Compiler.Tests.TestClass::get_PublicStaticStringProperty()"));

        TestUtils.EmitExpressionAndVerify(
            "Todl.Compiler.Tests.TestClass.Instance.PublicIntProperty",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Callvirt, "System.Int32 Todl.Compiler.Tests.TestClass::get_PublicIntProperty()"));

        TestUtils.EmitExpressionAndVerify(
            "Todl.Compiler.Tests.TestClass.Instance.PublicStringProperty",
            TestInstruction.Create(OpCodes.Ldsfld, "Todl.Compiler.Tests.TestClass Instance"),
            TestInstruction.Create(OpCodes.Callvirt, "System.String Todl.Compiler.Tests.TestClass::get_PublicStringProperty()"));
    }
}
