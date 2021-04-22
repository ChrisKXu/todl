using System.Collections.Generic;

namespace Todl.CodeAnalysis
{
    public sealed class SyntaxToken
    {
        public SyntaxKind Kind { get; }
        public string Text { get; }
        public int Position { get; }
        public IReadOnlyCollection<SyntaxTrivia> LeadingTrivia { get; }
        public IReadOnlyCollection<SyntaxTrivia> TrailingTrivia { get; }

        public SyntaxToken(
            SyntaxKind kind,
            string text,
            int position,
            IReadOnlyCollection<SyntaxTrivia> leadingTrivia,
            IReadOnlyCollection<SyntaxTrivia> trailingTrivia)
        {
            this.Kind = kind;
            this.Text = text;
            this.Position = position;
            this.LeadingTrivia = leadingTrivia;
            this.TrailingTrivia = trailingTrivia;
        }
    }
}