using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis
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