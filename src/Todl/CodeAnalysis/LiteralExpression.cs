using System.Collections.Generic;

namespace Todl.CodeAnalysis
{
    public class LiteralExpression : Expression
    {
        public SyntaxToken LiteralToken { get; }

        public string Text => this.LiteralToken.Text;

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