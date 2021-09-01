using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
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
            tokens.Count.Should().Be(6); // '1', '+', '2', '+', '3' and EndOfFileToken
        }

        [Theory]
        [InlineData(SyntaxKind.PlusToken, "+")]
        [InlineData(SyntaxKind.PlusPlusToken, "++")]
        [InlineData(SyntaxKind.PlusEqualsToken, "+=")]
        [InlineData(SyntaxKind.MinusToken, "-")]
        [InlineData(SyntaxKind.MinusMinusToken, "--")]
        [InlineData(SyntaxKind.MinusEqualsToken, "-=")]
        [InlineData(SyntaxKind.StarToken, "*")]
        [InlineData(SyntaxKind.StarEqualsToken, "*=")]
        [InlineData(SyntaxKind.SlashToken, "/")]
        [InlineData(SyntaxKind.SlashEqualsToken, "/=")]
        [InlineData(SyntaxKind.OpenParenthesisToken, "(")]
        [InlineData(SyntaxKind.CloseParenthesisToken, ")")]
        [InlineData(SyntaxKind.EqualsToken, "=")]
        [InlineData(SyntaxKind.EqualsEqualsToken, "==")]
        [InlineData(SyntaxKind.BangToken, "!")]
        [InlineData(SyntaxKind.BangEqualsToken, "!=")]
        [InlineData(SyntaxKind.AmpersandToken, "&")]
        [InlineData(SyntaxKind.AmpersandAmpersandToken, "&&")]
        [InlineData(SyntaxKind.PipeToken, "|")]
        [InlineData(SyntaxKind.PipePipeToken, "||")]
        [InlineData(SyntaxKind.DotToken, ".")]
        [InlineData(SyntaxKind.CommaToken, ",")]
        [InlineData(SyntaxKind.TrueKeywordToken, "true")]
        [InlineData(SyntaxKind.FalseKeywordToken, "false")]
        [InlineData(SyntaxKind.OpenBraceToken, "{")]
        [InlineData(SyntaxKind.CloseBraceToken, "}")]
        [InlineData(SyntaxKind.SemicolonToken, ";")]
        [InlineData(SyntaxKind.ColonToken, ":")]
        [InlineData(SyntaxKind.LetKeywordToken, "let")]
        [InlineData(SyntaxKind.ConstKeywordToken, "const")]
        [InlineData(SyntaxKind.ImportKeywordToken, "import")]
        [InlineData(SyntaxKind.FromKeywordToken, "from")]
        public void TestSingleToken(SyntaxKind kind, string text)
        {
            var sourceText = SourceText.FromString(text);
            var syntaxTree = new SyntaxTree(sourceText);
            var lexer = new Lexer(syntaxTree);
            lexer.Lex();

            lexer.SyntaxTokens.Count.Should().Be(2); // the expected token + EndOfFileToken
            lexer.Diagnostics.Should().BeEmpty();

            var token = lexer.SyntaxTokens[0];
            token.Kind.Should().Be(kind);
            token.Text.Should().Be(text);
        }

        [Theory]
        [InlineData("\"\"")]
        [InlineData("@\"\"")]
        [InlineData("\"abcd\"")]
        [InlineData("@\"abcd\"")]
        [InlineData("\"ab\\tcd\"")]
        [InlineData("@\"ab\\tcd\"")]
        [InlineData("\"ab\\\"cd\"")]
        [InlineData("@\"ab\\\"cd\"")]
        public void TestStringToken(string text)
        {
            var sourceText = SourceText.FromString(text);
            var syntaxTree = new SyntaxTree(sourceText);
            var lexer = new Lexer(syntaxTree);
            lexer.Lex();

            lexer.SyntaxTokens.Count.Should().Be(2); // the expected token + EndOfFileToken
            lexer.Diagnostics.Should().BeEmpty();

            var token = lexer.SyntaxTokens[0];
            token.Kind.Should().Be(SyntaxKind.StringToken);
            token.Text.Should().Be(text);
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
