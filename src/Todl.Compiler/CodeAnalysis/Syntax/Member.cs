namespace Todl.Compiler.CodeAnalysis.Syntax;

public abstract class Member : SyntaxNode { }

public sealed partial class Parser
{
    private Member ParseMember()
    {
        if (Current.Kind == SyntaxKind.ConstKeywordToken || Current.Kind == SyntaxKind.LetKeywordToken)
        {
            return ParseVariableDeclarationMember();
        }

        return ParseFunctionDeclarationMember();
    }
}
