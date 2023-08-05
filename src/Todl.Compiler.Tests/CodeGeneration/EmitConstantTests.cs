using FluentAssertions;
using Mono.Cecil.Cil;
using Todl.Compiler.CodeAnalysis.Binding;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class EmitConstantTests
{
    [Fact]
    public void TestEmitInt32Constant()
    {
        TestUtils.EmitExpressionAndVerify("0", TestInstruction.Create(OpCodes.Ldc_I4_0));
        TestUtils.EmitExpressionAndVerify("1", TestInstruction.Create(OpCodes.Ldc_I4_1));
        TestUtils.EmitExpressionAndVerify("2", TestInstruction.Create(OpCodes.Ldc_I4_2));
        TestUtils.EmitExpressionAndVerify("3", TestInstruction.Create(OpCodes.Ldc_I4_3));
        TestUtils.EmitExpressionAndVerify("4", TestInstruction.Create(OpCodes.Ldc_I4_4));
        TestUtils.EmitExpressionAndVerify("5", TestInstruction.Create(OpCodes.Ldc_I4_5));
        TestUtils.EmitExpressionAndVerify("6", TestInstruction.Create(OpCodes.Ldc_I4_6));
        TestUtils.EmitExpressionAndVerify("7", TestInstruction.Create(OpCodes.Ldc_I4_7));
        TestUtils.EmitExpressionAndVerify("8", TestInstruction.Create(OpCodes.Ldc_I4_8));
        TestUtils.EmitExpressionAndVerify("123", TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)123));
        TestUtils.EmitExpressionAndVerify("1024", TestInstruction.Create(OpCodes.Ldc_I4, 1024));
    }

    [Fact]
    public void TestEmitUInt32Constant()
    {
        TestUtils.EmitExpressionAndVerify("0u", TestInstruction.Create(OpCodes.Ldc_I4_0));
        TestUtils.EmitExpressionAndVerify("1u", TestInstruction.Create(OpCodes.Ldc_I4_1));
        TestUtils.EmitExpressionAndVerify("2u", TestInstruction.Create(OpCodes.Ldc_I4_2));
        TestUtils.EmitExpressionAndVerify("3u", TestInstruction.Create(OpCodes.Ldc_I4_3));
        TestUtils.EmitExpressionAndVerify("4u", TestInstruction.Create(OpCodes.Ldc_I4_4));
        TestUtils.EmitExpressionAndVerify("5u", TestInstruction.Create(OpCodes.Ldc_I4_5));
        TestUtils.EmitExpressionAndVerify("6u", TestInstruction.Create(OpCodes.Ldc_I4_6));
        TestUtils.EmitExpressionAndVerify("7u", TestInstruction.Create(OpCodes.Ldc_I4_7));
        TestUtils.EmitExpressionAndVerify("8u", TestInstruction.Create(OpCodes.Ldc_I4_8));
        TestUtils.EmitExpressionAndVerify("100u", TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)100));
        TestUtils.EmitExpressionAndVerify("1024u", TestInstruction.Create(OpCodes.Ldc_I4, 1024));
        TestUtils.EmitExpressionAndVerify("3000000000", TestInstruction.Create(OpCodes.Ldc_I4, -1294967296));
        TestUtils.EmitExpressionAndVerify("3000000000u", TestInstruction.Create(OpCodes.Ldc_I4, -1294967296));
    }

    [Fact]
    public void TestEmitInt64Constant()
    {
        TestUtils.EmitExpressionAndVerify("0l", TestInstruction.Create(OpCodes.Ldc_I4_0), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("1l", TestInstruction.Create(OpCodes.Ldc_I4_1), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("2l", TestInstruction.Create(OpCodes.Ldc_I4_2), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("3l", TestInstruction.Create(OpCodes.Ldc_I4_3), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("4l", TestInstruction.Create(OpCodes.Ldc_I4_4), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("5l", TestInstruction.Create(OpCodes.Ldc_I4_5), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("6l", TestInstruction.Create(OpCodes.Ldc_I4_6), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("7l", TestInstruction.Create(OpCodes.Ldc_I4_7), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("8l", TestInstruction.Create(OpCodes.Ldc_I4_8), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("100l", TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)100), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("1024l", TestInstruction.Create(OpCodes.Ldc_I4, 1024), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("3000000000l", TestInstruction.Create(OpCodes.Ldc_I4, -1294967296), TestInstruction.Create(OpCodes.Conv_U8));
        TestUtils.EmitExpressionAndVerify("123456789123", TestInstruction.Create(OpCodes.Ldc_I8, 123456789123));
        TestUtils.EmitExpressionAndVerify("123456789123l", TestInstruction.Create(OpCodes.Ldc_I8, 123456789123));
    }

    [Fact]
    public void TestEmitUInt64Constant()
    {
        TestUtils.EmitExpressionAndVerify("0ul", TestInstruction.Create(OpCodes.Ldc_I4_0), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("1ul", TestInstruction.Create(OpCodes.Ldc_I4_1), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("2ul", TestInstruction.Create(OpCodes.Ldc_I4_2), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("3ul", TestInstruction.Create(OpCodes.Ldc_I4_3), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("4ul", TestInstruction.Create(OpCodes.Ldc_I4_4), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("5ul", TestInstruction.Create(OpCodes.Ldc_I4_5), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("6ul", TestInstruction.Create(OpCodes.Ldc_I4_6), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("7ul", TestInstruction.Create(OpCodes.Ldc_I4_7), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("8ul", TestInstruction.Create(OpCodes.Ldc_I4_8), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("100ul", TestInstruction.Create(OpCodes.Ldc_I4_S, (sbyte)100), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("1024ul", TestInstruction.Create(OpCodes.Ldc_I4, 1024), TestInstruction.Create(OpCodes.Conv_I8));
        TestUtils.EmitExpressionAndVerify("3000000000ul", TestInstruction.Create(OpCodes.Ldc_I4, -1294967296), TestInstruction.Create(OpCodes.Conv_U8));
        TestUtils.EmitExpressionAndVerify("123456789123ul", TestInstruction.Create(OpCodes.Ldc_I8, 123456789123));
    }

    [Theory]
    [InlineData("0f", 0f)]
    [InlineData("123f", 123f)]
    [InlineData("123.456f", 123.456f)]
    [InlineData("123.456F", 123.456F)]
    [InlineData(".456f", .456f)]
    public void TestEmitFloatConstant(string input, float value)
    {
        TestUtils.EmitExpressionAndVerify(input, TestInstruction.Create(OpCodes.Ldc_R4, value));
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
        TestUtils.EmitExpressionAndVerify(input, TestInstruction.Create(OpCodes.Ldc_R8, value));
    }

    [Fact]
    public void TestEmitBooleanConstant()
    {
        TestUtils.EmitExpressionAndVerify("true", TestInstruction.Create(OpCodes.Ldc_I4_1));
        TestUtils.EmitExpressionAndVerify("false", TestInstruction.Create(OpCodes.Ldc_I4_0));
    }

    [Fact]
    public void TestEmitStringConstant()
    {
        TestUtils.EmitExpressionAndVerify("\"abc\"", TestInstruction.Create(OpCodes.Ldstr, "abc"));
    }
}
