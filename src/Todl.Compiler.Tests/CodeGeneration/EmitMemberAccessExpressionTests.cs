using FluentAssertions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitMemberAccessExpressionTests
{
    [Fact]
    public void TestEmitClrFieldExpression()
    {
        var boundClrFieldAccessExpression = TestUtils.BindExpression<BoundClrFieldAccessExpression>("bool.FalseString");
        var emitter = new TestEmitter();
        emitter.EmitExpression(boundClrFieldAccessExpression);

        var instructions = emitter.ILProcessor.Body.Instructions;
        instructions.Count.Should().Be(1);
        instructions[0].OpCode.Should().Be(OpCodes.Ldsfld);

        var operand = instructions[0].Operand.As<FieldReference>();
        operand.Name.Should().Be(nameof(bool.FalseString));
        operand.FieldType.FullName.Should().Be(typeof(string).FullName);
    }

    [Fact]
    public void TestEmitClrPropertyExpression()
    {
        var boundClrPropertyAccessExpression = TestUtils.BindExpression<BoundClrPropertyAccessExpression>("\"abc\".Length");
        var emitter = new TestEmitter();
        emitter.EmitExpression(boundClrPropertyAccessExpression);

        var instructions = emitter.ILProcessor.Body.Instructions;
        instructions.Count.Should().Be(2);

        var ldstr = instructions[0];
        ldstr.OpCode.Should().Be(OpCodes.Ldstr);
        ldstr.Operand.Should().Be("abc");

        var call = instructions[1];
        call.OpCode.Should().Be(OpCodes.Call);

        var operand = call.Operand.As<MethodReference>();
        operand.Name.Should().Be("get_Length");
        operand.ReturnType.FullName.Should().Be(typeof(int).FullName);
        operand.Parameters.Should().BeEmpty();
        operand.DeclaringType.FullName.Should().Be(typeof(string).FullName);
    }
}
