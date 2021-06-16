namespace Todl.Compiler.CodeAnalysis
{
    public enum SyntaxKind
    {
        // Tokens
        BadToken,
        EndOfFileToken,
        NumberToken,
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
        LeftParenthesisToken,
        RightParenthesisToken,
        EqualsToken,
        EqualsEqualsToken,
        BangToken,
        BangEqualsToken,

        // Trivia
        WhitespaceTrivia,
        LineBreakTrivia,
    }
}