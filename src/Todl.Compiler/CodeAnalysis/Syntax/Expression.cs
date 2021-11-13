using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public abstract class Expression : SyntaxNode
    {
        protected Expression(SyntaxTree syntaxTree) : base(syntaxTree)
        {
        }
    }

    public sealed partial class Parser
    {
        internal Expression ParseExpression()
        {
            return ParseBinaryExpression();
        }
    }
}
