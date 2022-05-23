namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public abstract class Statement : SyntaxNode { }

    public sealed partial class Parser
    {
        internal Statement ParseStatement()
        {
            return Current.Kind switch
            {
                SyntaxKind.OpenBraceToken => ParseBlockStatement(),
                SyntaxKind.ConstKeywordToken or SyntaxKind.LetKeywordToken => ParseVariableDeclarationStatement(),
                SyntaxKind.ReturnKeywordToken => ParseReturnStatement(),
                SyntaxKind.IfKeywordToken or SyntaxKind.UnlessKeywordToken => ParseIfUnlessStatement(),
                _ => ParseExpressionStatement()
            };
        }
    }
}
