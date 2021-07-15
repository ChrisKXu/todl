using System.Collections.Generic;
using Todl.Compiler.Utilities;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public abstract class SyntaxNode : ITreeWalkable<SyntaxNode>
    {
        public SyntaxTree SyntaxTree { get; }

        protected SyntaxNode(SyntaxTree syntaxTree)
        {
            this.SyntaxTree = syntaxTree;
        }

        public abstract IEnumerable<SyntaxNode> GetChildren();
    }
}
