using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

// Here are the rules for if and unless statements
//   * Starts with "if" or "unless" keyword
//   * No "()"s around conditions
//   * Inner statements are always enclosed by "{}" even for single liners
//   * "if" statements can only have "else if" clauses, and "unless" statements can only have "else unless" clauses
//   * there can be at most one bare "else" clauses and it must be after all other else clauses
public sealed class IfUnlessStatement : Statement
{
    public SyntaxToken IfOrUnlessToken { get; internal init; }
    public Expression ConditionExpression { get; internal init; }
    public BlockStatement BlockStatement { get; internal init; }
    public IReadOnlyList<ElseClause> ElseClauses { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromTextSpans(IfOrUnlessToken.Text, BlockStatement.Text);
}

public sealed class ElseClause : SyntaxNode
{
    public SyntaxToken ElseToken { get; internal init; }
    public SyntaxToken? IfOrUnlessToken { get; internal init; }
    public Expression ConditionExpression { get; internal init; }
    public BlockStatement BlockStatement { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromTextSpans(ElseToken.Text, BlockStatement.Text);
}

public sealed partial class Parser
{
    private IfUnlessStatement ParseIfUnlessStatement()
    {
        var ifOrUnlessToken =
            Current.Kind == SyntaxKind.IfKeywordToken
                ? ExpectToken(SyntaxKind.IfKeywordToken)
                : ExpectToken(SyntaxKind.UnlessKeywordToken);
        var conditionExpression = ParseExpression();
        var blockStatement = ParseBlockStatement();
        var elseClauses = new List<ElseClause>();

        while (Current.Kind == SyntaxKind.ElseKeywordToken)
        {
            var elseClause = ParseElseClause();
            elseClauses.Add(elseClause);
        }

        return new()
        {
            SyntaxTree = syntaxTree,
            IfOrUnlessToken = ifOrUnlessToken,
            ConditionExpression = conditionExpression,
            BlockStatement = blockStatement,
            ElseClauses = elseClauses
        };
    }

    private ElseClause ParseElseClause()
    {
        var elseToken = ExpectToken(SyntaxKind.ElseKeywordToken);
        SyntaxToken? ifOrUnlessToken = null;
        Expression conditionExpression = null;

        if (Current.Kind == SyntaxKind.IfKeywordToken || Current.Kind == SyntaxKind.UnlessKeywordToken)
        {
            ifOrUnlessToken = ExpectToken(Current.Kind);
            conditionExpression = ParseExpression();
        }

        return new()
        {
            SyntaxTree = syntaxTree,
            ElseToken = elseToken,
            IfOrUnlessToken = ifOrUnlessToken,
            ConditionExpression = conditionExpression,
            BlockStatement = ParseBlockStatement()
        };
    }
}
