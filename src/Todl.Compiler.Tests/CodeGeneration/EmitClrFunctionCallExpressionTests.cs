using Mono.Cecil.Cil;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitClrFunctionCallExpressionTests
{
    [Fact]
    public void TestEmitCovariantOverloadMatch()
    {
        TestUtils.EmitExpressionAndVerify(
            "System::Object.ReferenceEquals(\"a\", \"b\")",
            TestInstruction.Create(OpCodes.Ldstr, "a"),
            TestInstruction.Create(OpCodes.Ldstr, "b"),
            TestInstruction.Create(OpCodes.Call, "System.Boolean System.Object::ReferenceEquals(System.Object,System.Object)"));
    }

    [Fact]
    public void TestEmitClosedGenericReturnType()
    {
        TestUtils.EmitStatementAndVerify(
            "let x = System::IO::Directory.EnumerateFiles(\".\");",
            TestInstruction.Create(OpCodes.Ldstr, "."),
            TestInstruction.Create(OpCodes.Call, "System.Collections.Generic.IEnumerable`1<System.String> System.IO.Directory::EnumerateFiles(System.String)"),
            TestInstruction.Create(OpCodes.Stloc_0));
    }

    [Fact]
    public void TestEmitArrayOfPrimitiveReturnType()
    {
        TestUtils.EmitStatementAndVerify(
            "let x = System::Environment.GetCommandLineArgs();",
            TestInstruction.Create(OpCodes.Call, "System.String[] System.Environment::GetCommandLineArgs()"),
            TestInstruction.Create(OpCodes.Stloc_0));
    }
}
