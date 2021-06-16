using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class SyntaxToken : SyntaxNode
    {
        public SyntaxKind Kind { get; }
        public TextSpan Text { get; }
        public IReadOnlyCollection<SyntaxTrivia> LeadingTrivia { get; }
        public IReadOnlyCollection<SyntaxTrivia> TrailingTrivia { get; }

        public SyntaxToken(
            SyntaxTree syntaxTree,
            SyntaxKind kind,
            TextSpan text,
            IReadOnlyCollection<SyntaxTrivia> leadingTrivia,
            IReadOnlyCollection<SyntaxTrivia> trailingTrivia)
            : base(syntaxTree)
        {
            this.Kind = kind;
            this.Text = text;
            this.LeadingTrivia = leadingTrivia;
            this.TrailingTrivia = trailingTrivia;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield break;
        }

        public TextLocation GetTextLocation() => new TextLocation(this.SyntaxTree.SourceText, this.Text);
    }
}