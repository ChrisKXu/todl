using System.Collections.Generic;

namespace Todl.CodeAnalysis
{
    public abstract class SyntaxNode
    {
        public SyntaxTree SyntaxTree { get; }

        protected SyntaxNode(SyntaxTree syntaxTree)
        {
            this.SyntaxTree = syntaxTree;
        }

        public abstract IEnumerable<SyntaxNode> GetChildren();
    }
}