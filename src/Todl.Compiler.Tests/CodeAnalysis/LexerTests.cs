using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis
{
    public sealed class LexerTests
    {
        [Fact]
        public void TestLexerBasics()
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

        [Fact]
        public void TestLexerWithDiagnostics()
        {
            var sourceText = SourceText.FromString("1+    2 ^+3");
            var syntaxTree = new SyntaxTree(sourceText);
            var lexer = new Lexer(syntaxTree);
            lexer.Lex();

            var diagnostics = lexer.Diagnostics;

            diagnostics.Should().NotBeEmpty();
            diagnostics.Count.Should().Be(1);
            diagnostics[0].TextLocation.TextSpan.Start.Should().Be(8);
            diagnostics[0].TextLocation.TextSpan.Length.Should().Be(0);
        }
    }
}