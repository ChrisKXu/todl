using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis
{
    public sealed partial class LexerTests
    {
        private SyntaxToken LexSingle(string text)
        {
            var lexer = new Lexer() { SourceText = SourceText.FromString(text) };
            lexer.Lex();

            var tokens = lexer.SyntaxTokens;
            tokens.Count.Should().Be(2);
            tokens.Last().Kind.Should().Be(SyntaxKind.EndOfFileToken);
            lexer.SyntaxTokens[0].GetDiagnostics().Should().BeEmpty();

            return tokens.First();
        }

        [Fact]
        public void TestLexerBasics()
        {
            var sourceText = SourceText.FromString("1+    2 +3");
            var lexer = new Lexer() { SourceText = sourceText };
            lexer.Lex();

            var tokens = lexer.SyntaxTokens;
            tokens.Count.Should().Be(6); // '1', '+', '2', '+', '3' and EndOfFileToken
            tokens.SelectMany(t => t.GetDiagnostics()).Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(GetTestSingleTokenData))]
        public void TestSingleToken(SyntaxKind kind, string text)
        {
            var token = LexSingle(text);
            token.Kind.Should().Be(kind);
            token.Text.Should().Be(text);
        }

        [Fact]
        public void SingleTokenTestDataShouldCoverAllTokenKinds()
        {
            var actualTokenKinds = singleTokenTestData.Keys.ToHashSet();
            var exemptions = new SyntaxKind[]
            {
                SyntaxKind.BadToken,
                SyntaxKind.EndOfFileToken,
                SyntaxKind.StringToken,
                SyntaxKind.NumberToken,
                SyntaxKind.IdentifierToken,
                SyntaxKind.WhitespaceTrivia,
                SyntaxKind.LineBreakTrivia,
                SyntaxKind.SingleLineCommentTrivia
            }.ToHashSet();

            var uncoveredKinds = Enum.GetValues<SyntaxKind>().Except(actualTokenKinds.Union(exemptions));
            uncoveredKinds.Should().BeEmpty();
        }

        public static IEnumerable<object[]> GetTestSingleTokenData()
            => singleTokenTestData.Select(kv => new object[] { kv.Key, kv.Value });

        private static readonly Dictionary<SyntaxKind, string> singleTokenTestData = new()
        {
            { SyntaxKind.PlusToken, "+" },
            { SyntaxKind.PlusPlusToken, "++" },
            { SyntaxKind.PlusEqualsToken, "+=" },
            { SyntaxKind.MinusToken, "-" },
            { SyntaxKind.MinusMinusToken, "--" },
            { SyntaxKind.MinusEqualsToken, "-=" },
            { SyntaxKind.StarToken, "*" },
            { SyntaxKind.StarEqualsToken, "*=" },
            { SyntaxKind.SlashToken, "/" },
            { SyntaxKind.SlashEqualsToken, "/=" },
            { SyntaxKind.OpenParenthesisToken, "(" },
            { SyntaxKind.CloseParenthesisToken, ")" },
            { SyntaxKind.EqualsToken, "=" },
            { SyntaxKind.EqualsEqualsToken, "==" },
            { SyntaxKind.BangToken, "!" },
            { SyntaxKind.BangEqualsToken, "!=" },
            { SyntaxKind.LessThanToken, "<" },
            { SyntaxKind.LessThanOrEqualsToken, "<=" },
            { SyntaxKind.GreaterThanToken, ">" },
            { SyntaxKind.GreaterThanOrEqualsToken, ">=" },
            { SyntaxKind.AmpersandToken, "&" },
            { SyntaxKind.AmpersandAmpersandToken, "&&" },
            { SyntaxKind.PipeToken, "|" },
            { SyntaxKind.PipePipeToken, "||" },
            { SyntaxKind.TildeToken, "~" },
            { SyntaxKind.DotToken, "." },
            { SyntaxKind.CommaToken, "," },
            { SyntaxKind.TrueKeywordToken, "true" },
            { SyntaxKind.FalseKeywordToken, "false" },
            { SyntaxKind.OpenBraceToken, "{" },
            { SyntaxKind.CloseBraceToken, "}" },
            { SyntaxKind.OpenBracketToken, "[" },
            { SyntaxKind.CloseBracketToken, "]" },
            { SyntaxKind.SemicolonToken, ";" },
            { SyntaxKind.ColonToken, ":" },
            { SyntaxKind.LetKeywordToken, "let" },
            { SyntaxKind.ConstKeywordToken, "const" },
            { SyntaxKind.ImportKeywordToken, "import" },
            { SyntaxKind.FromKeywordToken, "from" },
            { SyntaxKind.NewKeywordToken, "new" },
            { SyntaxKind.BoolKeywordToken, "bool" },
            { SyntaxKind.ByteKeywordToken, "byte" },
            { SyntaxKind.CharKeywordToken, "char" },
            { SyntaxKind.IntKeywordToken, "int" },
            { SyntaxKind.LongKeywordToken, "long" },
            { SyntaxKind.StringKeywordToken, "string" },
            { SyntaxKind.VoidKeywordToken, "void" },
            { SyntaxKind.ReturnKeywordToken, "return" },
            { SyntaxKind.IfKeywordToken, "if" },
            { SyntaxKind.UnlessKeywordToken, "unless" },
            { SyntaxKind.ElseKeywordToken, "else" },
            { SyntaxKind.WhileKeywordToken, "while" },
            { SyntaxKind.UntilKeywordToken, "until" },
            { SyntaxKind.BreakKeywordToken, "break" },
            { SyntaxKind.ContinueKeywordToken, "continue" }
        };

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
            var token = LexSingle(text);
            token.Kind.Should().Be(SyntaxKind.StringToken);
            token.Text.Should().Be(text);
        }

        [Fact]
        public void TestLexerWithDiagnostics()
        {
            var sourceText = SourceText.FromString("1+    2 ^+3");
            var lexer = new Lexer() { SourceText = sourceText };
            lexer.Lex();

            var diagnostics = lexer.SyntaxTokens.SelectMany(t => t.GetDiagnostics()).ToList();

            diagnostics.Should().NotBeEmpty();
            diagnostics.Count.Should().Be(1);
            diagnostics[0].TextLocation.TextSpan.Start.Should().Be(8);
            diagnostics[0].TextLocation.TextSpan.Length.Should().Be(0);
        }
    }
}
