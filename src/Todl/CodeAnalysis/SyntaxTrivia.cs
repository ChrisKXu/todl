using Todl.CodeAnalysis.Text;

namespace Todl.CodeAnalysis
{
    public sealed class SyntaxTrivia
    {
        public SyntaxKind Kind { get; }
        public TextSpan Text { get; }

        public SyntaxTrivia(SyntaxKind kind, TextSpan text)
        {
            Kind = kind;
            Text = text;
        }
    }
}