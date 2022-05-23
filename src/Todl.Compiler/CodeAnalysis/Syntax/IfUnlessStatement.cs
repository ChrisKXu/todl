using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

// Here are the rules for if and unless statements
//   * Starts with "if" or "unless" keyword
//   * No "()"s around conditions
//   * Inner statements are always enclosed by "{}" even for single liners
//   * "if" statements can only have "else if" clauses, and "unless" statements can only have "else unless" clauses
public sealed class IfUnlessStatement : Statement
{
    public SyntaxToken IfOrUnlessToken { get; internal init; }
    public Expression ConditionExpression { get; internal init; }
    public BlockStatement BlockStatement { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromTextSpans(IfOrUnlessToken.Text, BlockStatement.Text);
}

public sealed partial class Parser
{
    private IfUnlessStatement ParseIfUnlessStatement()
    {
        var ifOrUnlessToken =
            Current.Kind == SyntaxKind.IfKeywordToken
                ? ExpectToken(SyntaxKind.IfKeywordToken)
                : ExpectToken(SyntaxKind.UnlessKeywordToken);

        return new()
        {
            SyntaxTree = syntaxTree,
            IfOrUnlessToken = ifOrUnlessToken,
            ConditionExpression = ParseExpression(),
            BlockStatement = ParseBlockStatement()
        };
    }
}
