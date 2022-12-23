using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed partial class LexerTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("00")]
    [InlineData("123")]
    [InlineData(".456")]
    [InlineData("234.567")]
    [InlineData("123.45632434234234234234234234324234234234")]
    [InlineData("12332434234234234234234234324234234234.456")]
    [InlineData("12332434234234234234234234324234234234.45623423423423423423423423423423423423")]
    [InlineData("100u")]
    [InlineData("100U")]
    [InlineData("100l")]
    [InlineData("100L")]
    [InlineData("100ul")]
    [InlineData("100UL")]
    [InlineData("123.456f")]
    [InlineData("123.456F")]
    [InlineData("123.456d")]
    [InlineData("123.456D")]
    public void TestNumericLiterals(string text)
    {
        var token = LexSingle(text);
        token.Kind.Should().Be(SyntaxKind.NumberToken);
        token.Text.Should().Be(text);
    }

    [Theory]
    [InlineData("0b0")]
    [InlineData("0b00")]
    [InlineData("0b0100")]
    [InlineData("0b0100u")]
    [InlineData("0b0100U")]
    [InlineData("0b0100l")]
    [InlineData("0b0100L")]
    [InlineData("0b0100ul")]
    [InlineData("0b0100UL")]
    [InlineData("0b01001010101100001111")]
    public void TestBinaryNumericLiterals(string text)
    {
        var token = LexSingle(text);
        token.Kind.Should().Be(SyntaxKind.NumberToken);
        token.Text.Should().Be(text);
    }

    [Theory]
    [InlineData("0x0")]
    [InlineData("0x00")]
    [InlineData("0x123")]
    [InlineData("0x123u")]
    [InlineData("0x123U")]
    [InlineData("0x123l")]
    [InlineData("0x123L")]
    [InlineData("0x123ul")]
    [InlineData("0x123UL")]
    [InlineData("0x123456789abcdef")]
    [InlineData("0x123456789ABCDEF")]
    public void TestHexadecimalNumericLiterals(string text)
    {
        var token = LexSingle(text);
        token.Kind.Should().Be(SyntaxKind.NumberToken);
        token.Text.Should().Be(text);
    }
}
