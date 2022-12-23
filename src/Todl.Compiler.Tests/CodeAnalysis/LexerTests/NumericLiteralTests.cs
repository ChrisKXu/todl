using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed partial class LexerTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("123")]
    [InlineData(".456")]
    [InlineData("234.567")]
    [InlineData("123.45632434234234234234234234324234234234")]
    [InlineData("12332434234234234234234234324234234234.456")]
    [InlineData("12332434234234234234234234324234234234.45623423423423423423423423423423423423")]
    public void TestNumericLiterals(string text)
    {
        var token = LexSingle(text);
        token.Kind.Should().Be(SyntaxKind.NumberToken);
        token.Text.Should().Be(text);
    }
}
