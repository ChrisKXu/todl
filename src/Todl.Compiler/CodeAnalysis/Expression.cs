namespace Todl.CodeAnalysis
{
    public abstract class Expression : SyntaxNode
    {
        protected Expression(SyntaxTree syntaxTree) : base(syntaxTree)
        {
        }
    }
}