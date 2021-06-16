namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public abstract class Expression : SyntaxNode
    {
        protected Expression(SyntaxTree syntaxTree) : base(syntaxTree)
        {
        }
    }
}