namespace Todl.CodeAnalysis
{
    public sealed class SyntaxTrivia
    {
        public SyntaxKind Kind { get; }
        public string Text { get; }
        public int Position { get; }

        public SyntaxTrivia(SyntaxKind kind, string text, int position)
        {
            Kind = kind;
            Text = text;
            Position = position;
        }
    }
}