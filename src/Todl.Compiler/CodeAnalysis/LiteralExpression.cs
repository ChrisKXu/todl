using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis
{
    public class LiteralExpression : Expression
    {
        public SyntaxToken LiteralToken { get; }

        public TextSpan Text => this.LiteralToken.Text;

        public LiteralExpression(SyntaxTree syntaxTree, SyntaxToken token) : base(syntaxTree)
        {
            this.LiteralToken = token;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return this.LiteralToken;
        }
    }
}