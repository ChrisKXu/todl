using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public readonly struct SyntaxToken
    {
        public SyntaxKind Kind { get; internal init; }
        public TextSpan Text { get; internal init; }
        public IReadOnlyCollection<SyntaxTrivia> LeadingTrivia { get; internal init; }
        public IReadOnlyCollection<SyntaxTrivia> TrailingTrivia { get; internal init; }
        public bool Missing { get; internal init; }
        public Diagnostic Diagnostic { get; internal init; }

        public TextLocation GetTextLocation() => new() { TextSpan = Text };
    }
}
