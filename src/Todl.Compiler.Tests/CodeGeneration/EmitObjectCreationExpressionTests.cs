using Mono.Cecil.Cil;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitObjectCreationExpressionTests
{
    [Fact]
    public void TestEmitObjectCreationExpression()
    {
        TestUtils.EmitExpressionAndVerify(
            "new System::Exception(\"boom\")",
            TestInstruction.Create(OpCodes.Ldstr, "boom"),
            TestInstruction.Create(OpCodes.Newobj, "System.Void System.Exception::.ctor(System.String)"));
    }
}
