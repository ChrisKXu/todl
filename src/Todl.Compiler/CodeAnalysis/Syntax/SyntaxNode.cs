using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Utilities;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public abstract class SyntaxNode : ITreeWalkable<SyntaxNode>
    {
        public SyntaxTree SyntaxTree { get; internal init; }

        public abstract IEnumerable<SyntaxNode> GetChildren();

        public virtual TextSpan Text
        {
            get
            {
                var children = GetChildren();

                return TextSpan.FromTextSpans(children.First().Text, children.Last().Text);
            }
        }
    }
}
