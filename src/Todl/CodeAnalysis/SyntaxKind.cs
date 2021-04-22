namespace Todl.CodeAnalysis
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

        // Trivia
        WhitespaceTrivia,
        LineBreakTrivia,
    }
}