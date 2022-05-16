﻿namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public enum SyntaxKind
    {
        // Tokens
        BadToken,
        EndOfFileToken,
        NumberToken,
        StringToken,
        PlusToken,
        PlusEqualsToken,
        PlusPlusToken,
        MinusToken,
        MinusEqualsToken,
        MinusMinusToken,
        StarToken,
        StarEqualsToken,
        SlashToken,
        SlashEqualsToken,
        OpenParenthesisToken,
        CloseParenthesisToken,
        OpenBraceToken,
        CloseBraceToken,
        OpenBracketToken,
        CloseBracketToken,
        SemicolonToken,
        ColonToken,
        EqualsToken,
        EqualsEqualsToken,
        BangToken,
        BangEqualsToken,
        AmpersandToken,
        AmpersandAmpersandToken,
        PipeToken,
        PipePipeToken,
        DotToken,
        CommaToken,
        IdentifierToken,

        // Keywords
        TrueKeywordToken,
        FalseKeywordToken,
        ConstKeywordToken,
        LetKeywordToken,
        ImportKeywordToken,
        FromKeywordToken,
        NewKeywordToken,
        BoolKeywordToken,
        ByteKeywordToken,
        CharKeywordToken,
        IntKeywordToken,
        LongKeywordToken,
        StringKeywordToken,
        VoidKeywordToken,
        ReturnKeywordToken,

        // Trivia
        WhitespaceTrivia,
        LineBreakTrivia
    }
}
