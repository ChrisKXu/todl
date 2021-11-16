using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public readonly struct SyntaxToken
    {
        public SyntaxKind Kind { get; }
        public TextSpan Text { get; }
        public IReadOnlyCollection<SyntaxTrivia> LeadingTrivia { get; }
        public IReadOnlyCollection<SyntaxTrivia> TrailingTrivia { get; }

        public SyntaxToken(
            SyntaxKind kind,
            TextSpan text,
            IReadOnlyCollection<SyntaxTrivia> leadingTrivia,
            IReadOnlyCollection<SyntaxTrivia> trailingTrivia)
        {
            this.Kind = kind;
            this.Text = text;
            this.LeadingTrivia = leadingTrivia;
            this.TrailingTrivia = trailingTrivia;
        }

        public TextLocation GetTextLocation() => new(Text.SourceText, Text);
    }
}
