using FluentAssertions;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed partial class EmitterTests
{
    [Fact]
    public void TestEmitInt32Constant()
    {
        TestEmitConstantCore("0", Instruction.Create(OpCodes.Ldc_I4_0));
        TestEmitConstantCore("1", Instruction.Create(OpCodes.Ldc_I4_1));
        TestEmitConstantCore("2", Instruction.Create(OpCodes.Ldc_I4_2));
        TestEmitConstantCore("3", Instruction.Create(OpCodes.Ldc_I4_3));
        TestEmitConstantCore("4", Instruction.Create(OpCodes.Ldc_I4_4));
        TestEmitConstantCore("5", Instruction.Create(OpCodes.Ldc_I4_5));
        TestEmitConstantCore("6", Instruction.Create(OpCodes.Ldc_I4_6));
        TestEmitConstantCore("7", Instruction.Create(OpCodes.Ldc_I4_7));
        TestEmitConstantCore("8", Instruction.Create(OpCodes.Ldc_I4_8));
        TestEmitConstantCore("123", Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)123));
        TestEmitConstantCore("1024", Instruction.Create(OpCodes.Ldc_I4, 1024));
    }

    [Fact]
    public void TestEmitUInt32Constant()
    {
        TestEmitConstantCore("0u", Instruction.Create(OpCodes.Ldc_I4_0));
        TestEmitConstantCore("1u", Instruction.Create(OpCodes.Ldc_I4_1));
        TestEmitConstantCore("2u", Instruction.Create(OpCodes.Ldc_I4_2));
        TestEmitConstantCore("3u", Instruction.Create(OpCodes.Ldc_I4_3));
        TestEmitConstantCore("4u", Instruction.Create(OpCodes.Ldc_I4_4));
        TestEmitConstantCore("5u", Instruction.Create(OpCodes.Ldc_I4_5));
        TestEmitConstantCore("6u", Instruction.Create(OpCodes.Ldc_I4_6));
        TestEmitConstantCore("7u", Instruction.Create(OpCodes.Ldc_I4_7));
        TestEmitConstantCore("8u", Instruction.Create(OpCodes.Ldc_I4_8));
        TestEmitConstantCore("100u", Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)100));
        TestEmitConstantCore("1024u", Instruction.Create(OpCodes.Ldc_I4, 1024));
        //TestEmitConstantCore("3000000000", Instruction.Create(OpCodes.Ldc_I4, -1294967296));
        TestEmitConstantCore("3000000000u", Instruction.Create(OpCodes.Ldc_I4, -1294967296));
    }

    [Fact]
    public void TestEmitInt64Constant()
    {
        TestEmitConstantCore("0l", Instruction.Create(OpCodes.Ldc_I4_0), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("1l", Instruction.Create(OpCodes.Ldc_I4_1), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("2l", Instruction.Create(OpCodes.Ldc_I4_2), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("3l", Instruction.Create(OpCodes.Ldc_I4_3), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("4l", Instruction.Create(OpCodes.Ldc_I4_4), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("5l", Instruction.Create(OpCodes.Ldc_I4_5), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("6l", Instruction.Create(OpCodes.Ldc_I4_6), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("7l", Instruction.Create(OpCodes.Ldc_I4_7), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("8l", Instruction.Create(OpCodes.Ldc_I4_8), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("100l", Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)100), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("1024l", Instruction.Create(OpCodes.Ldc_I4, 1024), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("3000000000l", Instruction.Create(OpCodes.Ldc_I4, -1294967296), Instruction.Create(OpCodes.Conv_U8));
        TestEmitConstantCore("123456789123", Instruction.Create(OpCodes.Ldc_I8, 123456789123));
        TestEmitConstantCore("123456789123l", Instruction.Create(OpCodes.Ldc_I8, 123456789123));
    }

    [Fact]
    public void TestEmitUInt64Constant()
    {
        TestEmitConstantCore("0ul", Instruction.Create(OpCodes.Ldc_I4_0), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("1ul", Instruction.Create(OpCodes.Ldc_I4_1), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("2ul", Instruction.Create(OpCodes.Ldc_I4_2), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("3ul", Instruction.Create(OpCodes.Ldc_I4_3), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("4ul", Instruction.Create(OpCodes.Ldc_I4_4), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("5ul", Instruction.Create(OpCodes.Ldc_I4_5), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("6ul", Instruction.Create(OpCodes.Ldc_I4_6), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("7ul", Instruction.Create(OpCodes.Ldc_I4_7), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("8ul", Instruction.Create(OpCodes.Ldc_I4_8), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("100ul", Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)100), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("1024ul", Instruction.Create(OpCodes.Ldc_I4, 1024), Instruction.Create(OpCodes.Conv_I8));
        TestEmitConstantCore("3000000000ul", Instruction.Create(OpCodes.Ldc_I4, -1294967296), Instruction.Create(OpCodes.Conv_U8));
        TestEmitConstantCore("123456789123ul", Instruction.Create(OpCodes.Ldc_I8, 123456789123));
    }

    [Theory]
    [InlineData("0f", 0f)]
    [InlineData("123f", 123f)]
    [InlineData("123.456f", 123.456f)]
    [InlineData("123.456F", 123.456F)]
    [InlineData(".456f", .456f)]
    public void TestEmitFloatConstant(string input, float value)
    {
        TestEmitConstantCore(input, Instruction.Create(OpCodes.Ldc_R4, value));
    }

    [Theory]
    [InlineData(".456", .456)]
    [InlineData("234.567", 234.567)]
    [InlineData("123.45632434234234234234234234324234234234", 123.45632434234234234234234234324234234234)]
    [InlineData("12332434234234234234234234324234234234.456", 12332434234234234234234234324234234234.456)]
    [InlineData("12332434234234234234234234324234234234.45623423423423423423423423423423423423", 12332434234234234234234234324234234234.45623423423423423423423423423423423423)]
    [InlineData("123d", 123d)]
    [InlineData("123.456d", 123.456d)]
    [InlineData("123.456D", 123.456D)]
    public void TestEmitDoubleConstant(string input, double value)
    {
        TestEmitConstantCore(input, Instruction.Create(OpCodes.Ldc_R8, value));
    }

    [Fact]
    public void TestEmitBooleanConstant()
    {
        TestEmitConstantCore("true", Instruction.Create(OpCodes.Ldc_I4_1));
        TestEmitConstantCore("false", Instruction.Create(OpCodes.Ldc_I4_0));
    }

    [Fact]
    public void TestEmitStringConstant()
    {
        TestEmitConstantCore("\"abc\"", Instruction.Create(OpCodes.Ldstr, "abc"));
    }

    private void TestEmitConstantCore(string input, params Instruction[] expectedInstructions)
    {
        var boundConstant = BindExpression<BoundConstant>(input);
        var emitter = new TestEmitter();
        emitter.EmitExpression(boundConstant);

        var instructions = emitter.ILProcessor.Body.Instructions;
        instructions.Should().HaveCount(expectedInstructions.Length);

        for (var i = 0; i != expectedInstructions.Length; ++i)
        {
            instructions[i].OpCode.Should().Be(expectedInstructions[i].OpCode);
            instructions[i].Operand.Should().Be(expectedInstructions[i].Operand);
        }
    }
}
