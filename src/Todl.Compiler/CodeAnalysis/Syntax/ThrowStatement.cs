using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class ThrowStatement : Statement
{
    public SyntaxToken ThrowKeywordToken { get; internal init; }
    public Expression Expression { get; internal init; }
    public SyntaxToken SemicolonToken { get; internal init; }

    public bool IsRethrow => Expression is null;

    public override TextSpan Text
        => TextSpan.FromBounds(ThrowKeywordToken.Span.Start, SemicolonToken.Span.End);
}

public sealed partial class Parser
{
    private ThrowStatement ParseThrowStatement()
    {
        var throwKeywordToken = ExpectToken(SyntaxKind.ThrowKeywordToken);
        Expression expression = null;

        if (Current.Kind != SyntaxKind.SemicolonToken)
        {
            expression = ParseExpression();
        }

        return new()
        {
            SyntaxTree = syntaxTree,
            ThrowKeywordToken = throwKeywordToken,
            Expression = expression,
            SemicolonToken = ExpectToken(SyntaxKind.SemicolonToken)
        };
    }
}
