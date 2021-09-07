namespace Todl.Compiler.CodeAnalysis.Syntax
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

        // Trivia
        WhitespaceTrivia,
        LineBreakTrivia,
    }
}
