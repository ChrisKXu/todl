using FluentAssertions;
using Todl.CodeAnalysis;
using Xunit;

namespace Todl.Tests.CodeAnalysis
{
    public sealed class LexerTests
    {
        [Fact]
        public void BasicTests()
        {
            var sourceText = SourceText.FromString("1+    2 +3");
            var lexer = new Lexer(sourceText);
            lexer.Lex();

            var tokens = lexer.SyntaxTokens;
            var diagnostics = lexer.Diagnostics;

            diagnostics.Should().BeEmpty();
            tokens.Count.Should().Equals(5);
        }
    }
}