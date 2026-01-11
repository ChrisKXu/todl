using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed partial class LexerTests
{
    private SyntaxToken LexSingle(string text)
    {
        var tokens = Lex(text);
        tokens.Length.Should().Be(2);
        tokens[0].GetDiagnostics().Should().BeEmpty();

        return tokens.First();
    }

    private ImmutableArray<SyntaxToken> Lex(string text)
    {
        var lexer = new Lexer() { SourceText = SourceText.FromString(text) };
        lexer.Lex();
        lexer.SyntaxTokens[^1].Kind.Should().Be(SyntaxKind.EndOfFileToken);

        return lexer.SyntaxTokens;
    }

    [Fact]
    public void TestLexerBasics()
    {
        var sourceText = SourceText.FromString("1+    2 +3");
        var lexer = new Lexer() { SourceText = sourceText };
        lexer.Lex();

        var tokens = lexer.SyntaxTokens;
        tokens.Length.Should().Be(6); // '1', '+', '2', '+', '3' and EndOfFileToken
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
        { SyntaxKind.PlusEqualsToken, "+=" },
        { SyntaxKind.MinusToken, "-" },
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
        { SyntaxKind.ColonColonToken, "::" },
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
    public void TestSingleLineCommentSimple()
    {
        var text = "// comment";
        var tokens = Lex(text);
        tokens.Length.Should().Be(1); // eof

        var eof = tokens[0];
        eof.Kind.Should().Be(SyntaxKind.EndOfFileToken);
        eof.LeadingTrivia.Length.Should().Be(1);
        eof.LeadingTrivia.Should().Contain(t => t.Kind == SyntaxKind.SingleLineCommentTrivia
            && t.Text.ToString() == text);
    }

    [Fact]
    public void TestSingleLineCommentsWithTokens()
    {
        var text = "//A\nreturn 0; //B";
        var tokens = Lex(text);
        tokens.Length.Should().Be(4); // return, 0, ;, eof

        var returnToken = tokens[0];
        returnToken.Kind.Should().Be(SyntaxKind.ReturnKeywordToken);
        returnToken.LeadingTrivia.Length.Should().Be(2); // comment, line break

        var a = returnToken.LeadingTrivia[0];
        a.Kind.Should().Be(SyntaxKind.SingleLineCommentTrivia);
        a.Text.ToString().Should().Be("//A");

        var commaToken = tokens[2];
        commaToken.TrailingTrivia.Length.Should().Be(2); // whitespace, comment

        var b = commaToken.TrailingTrivia[1];
        b.Kind.Should().Be(SyntaxKind.SingleLineCommentTrivia);
        b.Text.ToString().Should().Be("//B");
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

    [Theory]
    [InlineData("\"unterminated")]
    [InlineData("\"with\nnewline\"")]
    public void TestUnterminatedStringLiteral(string input)
    {
        var sourceText = SourceText.FromString(input);
        var lexer = new Lexer() { SourceText = sourceText };
        lexer.Lex();

        // Should contain a bad token for unterminated string
        lexer.SyntaxTokens.Should().Contain(t => t.Kind == SyntaxKind.BadToken);
    }

    [Fact]
    public void TestIdentifiersCannotStartWithDigit()
    {
        var tokens = Lex("123abc");

        // Should lex as a number followed by an identifier
        tokens.Length.Should().Be(3); // number, identifier, eof
        tokens[0].Kind.Should().Be(SyntaxKind.NumberToken);
        tokens[0].Text.ToString().Should().Be("123");
        tokens[1].Kind.Should().Be(SyntaxKind.IdentifierToken);
        tokens[1].Text.ToString().Should().Be("abc");
    }

    [Fact]
    public void TestEmptyInput()
    {
        var tokens = Lex("");

        tokens.Length.Should().Be(1);
        tokens[0].Kind.Should().Be(SyntaxKind.EndOfFileToken);
    }

    [Fact]
    public void TestWhitespaceOnlyInput()
    {
        var tokens = Lex("   \t\t   ");

        tokens.Length.Should().Be(1);
        tokens[0].Kind.Should().Be(SyntaxKind.EndOfFileToken);
        tokens[0].LeadingTrivia.Should().NotBeEmpty();
    }

    [Fact]
    public void TestMultipleOperators()
    {
        var tokens = Lex("+ - * / = == != < > <= >= && || & |");

        var operatorKinds = tokens.Take(tokens.Length - 1).Select(t => t.Kind).ToList();
        operatorKinds.Should().ContainInOrder(
            SyntaxKind.PlusToken,
            SyntaxKind.MinusToken,
            SyntaxKind.StarToken,
            SyntaxKind.SlashToken,
            SyntaxKind.EqualsToken,
            SyntaxKind.EqualsEqualsToken,
            SyntaxKind.BangEqualsToken,
            SyntaxKind.LessThanToken,
            SyntaxKind.GreaterThanToken,
            SyntaxKind.LessThanOrEqualsToken,
            SyntaxKind.GreaterThanOrEqualsToken,
            SyntaxKind.AmpersandAmpersandToken,
            SyntaxKind.PipePipeToken,
            SyntaxKind.AmpersandToken,
            SyntaxKind.PipeToken);
    }

    [Fact]
    public void TestVerbatimStringLiteral()
    {
        var token = LexSingle("@\"hello\\nworld\"");

        token.Kind.Should().Be(SyntaxKind.StringToken);
        token.Text.ToString().Should().Be("@\"hello\\nworld\"");
    }

    [Fact]
    public void TestStringWithEscapedQuote()
    {
        var token = LexSingle("\"hello\\\"world\"");

        token.Kind.Should().Be(SyntaxKind.StringToken);
        token.Text.ToString().Should().Be("\"hello\\\"world\"");
    }

    [Fact]
    public void TestColonColonToken()
    {
        var token = LexSingle("::");

        token.Kind.Should().Be(SyntaxKind.ColonColonToken);
        token.Text.ToString().Should().Be("::");
    }

    [Fact]
    public void TestCompoundAssignmentTokens()
    {
        var tokens = Lex("+= -= *= /=");

        tokens.Length.Should().Be(5); // 4 tokens + EOF
        tokens[0].Kind.Should().Be(SyntaxKind.PlusEqualsToken);
        tokens[1].Kind.Should().Be(SyntaxKind.MinusEqualsToken);
        tokens[2].Kind.Should().Be(SyntaxKind.StarEqualsToken);
        tokens[3].Kind.Should().Be(SyntaxKind.SlashEqualsToken);
    }

    [Fact]
    public void TestAllBracketTypes()
    {
        var tokens = Lex("() {} []");

        tokens.Length.Should().Be(7); // 6 tokens + EOF
        tokens[0].Kind.Should().Be(SyntaxKind.OpenParenthesisToken);
        tokens[1].Kind.Should().Be(SyntaxKind.CloseParenthesisToken);
        tokens[2].Kind.Should().Be(SyntaxKind.OpenBraceToken);
        tokens[3].Kind.Should().Be(SyntaxKind.CloseBraceToken);
        tokens[4].Kind.Should().Be(SyntaxKind.OpenBracketToken);
        tokens[5].Kind.Should().Be(SyntaxKind.CloseBracketToken);
    }

    [Fact]
    public void TestKeywordsCaseSensitive()
    {
        var tokens = Lex("if IF If");

        tokens.Length.Should().Be(4); // 3 tokens + EOF
        tokens[0].Kind.Should().Be(SyntaxKind.IfKeywordToken);
        tokens[1].Kind.Should().Be(SyntaxKind.IdentifierToken); // IF is not a keyword
        tokens[2].Kind.Should().Be(SyntaxKind.IdentifierToken); // If is not a keyword
    }

    [Theory]
    [InlineData("true", SyntaxKind.TrueKeywordToken)]
    [InlineData("false", SyntaxKind.FalseKeywordToken)]
    [InlineData("const", SyntaxKind.ConstKeywordToken)]
    [InlineData("let", SyntaxKind.LetKeywordToken)]
    [InlineData("import", SyntaxKind.ImportKeywordToken)]
    [InlineData("from", SyntaxKind.FromKeywordToken)]
    [InlineData("new", SyntaxKind.NewKeywordToken)]
    [InlineData("return", SyntaxKind.ReturnKeywordToken)]
    [InlineData("if", SyntaxKind.IfKeywordToken)]
    [InlineData("unless", SyntaxKind.UnlessKeywordToken)]
    [InlineData("else", SyntaxKind.ElseKeywordToken)]
    [InlineData("while", SyntaxKind.WhileKeywordToken)]
    [InlineData("until", SyntaxKind.UntilKeywordToken)]
    [InlineData("break", SyntaxKind.BreakKeywordToken)]
    [InlineData("continue", SyntaxKind.ContinueKeywordToken)]
    public void TestAllKeywords(string keyword, SyntaxKind expectedKind)
    {
        var token = LexSingle(keyword);

        token.Kind.Should().Be(expectedKind);
        token.Text.ToString().Should().Be(keyword);
    }

    [Theory]
    [InlineData("int", SyntaxKind.IntKeywordToken)]
    [InlineData("string", SyntaxKind.StringKeywordToken)]
    [InlineData("bool", SyntaxKind.BoolKeywordToken)]
    [InlineData("void", SyntaxKind.VoidKeywordToken)]
    [InlineData("byte", SyntaxKind.ByteKeywordToken)]
    [InlineData("char", SyntaxKind.CharKeywordToken)]
    [InlineData("long", SyntaxKind.LongKeywordToken)]
    public void TestBuiltInTypeKeywords(string typeName, SyntaxKind expectedKind)
    {
        var token = LexSingle(typeName);

        token.Kind.Should().Be(expectedKind);
        token.Text.ToString().Should().Be(typeName);
    }
}
