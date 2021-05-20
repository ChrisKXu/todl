using FluentAssertions;
using Todl.CodeAnalysis;
using Xunit;

namespace Todl.Tests.CodeAnalysis
{
    public sealed class LexerTests
    {
        [Fact]
        public void BasicLexerTests()
        {
            var sourceText = SourceText.FromString("1+    2 +3");
            var syntaxTree = new SyntaxTree(sourceText);
            var lexer = new Lexer(syntaxTree);
            lexer.Lex();

            var tokens = lexer.SyntaxTokens;
            var diagnostics = lexer.Diagnostics;

            diagnostics.Should().BeEmpty();
            tokens.Count.Should().Equals(5);
        }
    }
}