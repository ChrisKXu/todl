namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public abstract class Member : SyntaxNode { }

    public sealed partial class Parser
    {
        internal Member ParseMember()
        {
            if (Current.Kind == SyntaxKind.ConstKeywordToken || Current.Kind == SyntaxKind.LetKeywordToken)
            {
                return ParseVariableDeclarationMember();
            }

            return ParseFunctionDeclarationMember();
        }
    }
}
