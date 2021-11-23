using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class ReturnStatement : Statement
{
    public SyntaxToken ReturnKeywordToken { get; internal init; }
    public Expression ReturnValueExpression { get; internal init; }
    public SyntaxToken SemicolonToken { get; internal init; }

    public override TextSpan Text => TextSpan.FromTextSpans(ReturnKeywordToken.Text, SemicolonToken.Text);
}

public sealed partial class Parser
{
    private ReturnStatement ParseReturnStatement()
    {
        var returnKeywordToken = ExpectToken(SyntaxKind.ReturnKeywordToken);
        Expression returnValueExpression = null;

        if (Current.Kind != SyntaxKind.SemicolonToken)
        {
            returnValueExpression = ParseExpression();
        }

        var semicolonToken = ExpectToken(SyntaxKind.SemicolonToken);

        return new()
        {
            SyntaxTree = syntaxTree,
            ReturnKeywordToken = returnKeywordToken,
            ReturnValueExpression = returnValueExpression,
            SemicolonToken = semicolonToken
        };
    }
}
