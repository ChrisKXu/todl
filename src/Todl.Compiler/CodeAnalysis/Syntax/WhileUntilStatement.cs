using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class WhileUntilStatement : Statement
{
    public SyntaxToken WhileOrUntilToken { get; internal init; }
    public Expression ConditionExpression { get; internal init; }
    public BlockStatement BlockStatement { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromTextSpans(WhileOrUntilToken.Text, BlockStatement.Text);
}

public sealed partial class Parser
{
    private WhileUntilStatement ParseWhileUntilStatement()
    {
        var whileOrUntilToken =
            Current.Kind == SyntaxKind.WhileKeywordToken
                ? ExpectToken(SyntaxKind.WhileKeywordToken)
                : ExpectToken(SyntaxKind.UntilKeywordToken);

        return new()
        {
            SyntaxTree = syntaxTree,
            WhileOrUntilToken = whileOrUntilToken,
            ConditionExpression = ParseExpression(),
            BlockStatement = ParseBlockStatement()
        };
    }
}
