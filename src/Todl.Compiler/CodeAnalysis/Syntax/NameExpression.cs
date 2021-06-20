using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class NameExpression : Expression
    {
        public SyntaxToken IdentifierToken { get; }

        public NameExpression(SyntaxTree syntaxTree, SyntaxToken identifierToken)
            : base(syntaxTree)
        {
            this.IdentifierToken = identifierToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return this.IdentifierToken;
        }
    }
}
