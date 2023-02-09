using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public partial class BinderTests
{
    [Theory]
    [InlineData("\"\"", "")]
    [InlineData("\"abcd\"", "abcd")]
    [InlineData("\"ab\\\"cd\"", "ab\"cd")]
    [InlineData("@\"abcd\"", "abcd")]
    [InlineData("@\"ab\\\"cd\"", "ab\\\"cd")]
    public void TestBindStringConstant(string input, string expectedOutput)
    {
        var boundConstant = BindExpression<BoundConstant>(input);

        boundConstant.Should().NotBeNull();
        boundConstant.ResultType.Should().Be(builtInTypes.String);
        boundConstant.Value.Should().Be(expectedOutput);
    }

#pragma warning disable 0078

    [Theory]
    [InlineData("0", 0)]
    [InlineData("00", 0)]
    [InlineData("123", 123)]
    [InlineData("100u", 100u)]
    [InlineData("100U", 100U)]
    [InlineData("100l", 100l)]
    [InlineData("100L", 100L)]
    [InlineData("100ul", 100ul)]
    [InlineData("100UL", 100UL)]
    [InlineData("0b0", 0b0)]
    [InlineData("0b00", 0b00)]
    [InlineData("0b0100", 0b0100)]
    [InlineData("0b0100u", 0b0100u)]
    [InlineData("0b0100U", 0b0100U)]
    [InlineData("0b0100l", 0b0100l)]
    [InlineData("0b0100L", 0b0100L)]
    [InlineData("0b0100ul", 0b0100ul)]
    [InlineData("0b0100UL", 0b0100UL)]
    [InlineData("0b01001010101100001111", 0b01001010101100001111)]
    [InlineData("0x0", 0x0)]
    [InlineData("0x00", 0x00)]
    [InlineData("0x123", 0x123)]
    [InlineData("0x123u", 0x123u)]
    [InlineData("0x123U", 0x123U)]
    [InlineData("0x123l", 0x123l)]
    [InlineData("0x123L", 0x123L)]
    [InlineData("0x123ul", 0x123ul)]
    [InlineData("0x123UL", 0x123UL)]
    [InlineData("0x123456789abcdef", 0x123456789abcdef)]
    [InlineData("0x123456789ABCDEF", 0x123456789ABCDEF)]
    public void TestBindIntegerConstant(string input, object expectedValue)
    {
        var boundConstant = BindExpression<BoundConstant>(input);

        boundConstant.Should().NotBeNull();
        boundConstant.ResultType.As<ClrTypeSymbol>().ClrType.FullName.Should().Be(expectedValue.GetType().FullName);
        boundConstant.Value.Should().Be(expectedValue);
    }

#pragma warning restore 0078

    [Theory]
    [InlineData(".456", .456)]
    [InlineData(".456f", .456f)]
    [InlineData("234.567", 234.567)]
    [InlineData("123.45632434234234234234234234324234234234", 123.45632434234234234234234234324234234234)]
    [InlineData("12332434234234234234234234324234234234.456", 12332434234234234234234234324234234234.456)]
    [InlineData("12332434234234234234234234324234234234.45623423423423423423423423423423423423", 12332434234234234234234234324234234234.45623423423423423423423423423423423423)]
    [InlineData("123f", 123f)]
    [InlineData("123d", 123d)]
    [InlineData("123.456f", 123.456f)]
    [InlineData("123.456F", 123.456F)]
    [InlineData("123.456d", 123.456d)]
    [InlineData("123.456D", 123.456D)]
    public void TestBindFloatingPointConstant(string input, object expectedValue)
    {
        var boundConstant = BindExpression<BoundConstant>(input);

        boundConstant.Should().NotBeNull();
        boundConstant.ResultType.As<ClrTypeSymbol>().ClrType.FullName.Should().Be(expectedValue.GetType().FullName);
        boundConstant.Value.Should().Be(expectedValue);
    }
}
