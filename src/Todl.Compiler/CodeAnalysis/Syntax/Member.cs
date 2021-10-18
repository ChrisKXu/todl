namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public abstract class Member : SyntaxNode
    {
        protected Member(SyntaxTree syntaxTree) : base(syntaxTree) { }
    }

    public sealed partial class Parser
    {
        internal Member ParseMember()
        {
            return ParseVariableDeclarationMember();
        }
    }
}
