using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class SyntaxToken : SyntaxNode
    {
        public SyntaxKind Kind { get; }
        public override TextSpan Text { get; }
        public IReadOnlyCollection<SyntaxTrivia> LeadingTrivia { get; }
        public IReadOnlyCollection<SyntaxTrivia> TrailingTrivia { get; }

        public SyntaxToken(
            SyntaxTree syntaxTree,
            SyntaxKind kind,
            TextSpan text,
            IReadOnlyCollection<SyntaxTrivia> leadingTrivia,
            IReadOnlyCollection<SyntaxTrivia> trailingTrivia)
            : base()
        {
            this.SyntaxTree = syntaxTree;
            this.Kind = kind;
            this.Text = text;
            this.LeadingTrivia = leadingTrivia;
            this.TrailingTrivia = trailingTrivia;
        }

        public TextLocation GetTextLocation() => new(SyntaxTree.SourceText, Text);
    }
}
