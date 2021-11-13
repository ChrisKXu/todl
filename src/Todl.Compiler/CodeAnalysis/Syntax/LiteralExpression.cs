using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public class LiteralExpression : Expression
    {
        public SyntaxToken LiteralToken { get; }

        public override TextSpan Text => LiteralToken.Text;

        public LiteralExpression(SyntaxTree syntaxTree, SyntaxToken token) : base(syntaxTree)
        {
            LiteralToken = token;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return LiteralToken;
        }
    }
}
