namespace Todl.Compiler.CodeAnalysis.Syntax;

public abstract class Statement : SyntaxNode { }

public sealed partial class Parser
{
    internal Statement ParseStatement()
        => Current.Kind switch
        {
            SyntaxKind.OpenBraceToken => ParseBlockStatement(),
            SyntaxKind.ConstKeywordToken or SyntaxKind.LetKeywordToken => ParseVariableDeclarationStatement(),
            SyntaxKind.ReturnKeywordToken => ParseReturnStatement(),
            SyntaxKind.IfKeywordToken or SyntaxKind.UnlessKeywordToken => ParseIfUnlessStatement(),
            SyntaxKind.BreakKeywordToken => ParseBreakStatement(),
            SyntaxKind.ContinueKeywordToken => ParseContinueStatement(),
            SyntaxKind.WhileKeywordToken or SyntaxKind.UntilKeywordToken => ParseWhileUntilStatement(),
            _ => ParseExpressionStatement()
        };
}
