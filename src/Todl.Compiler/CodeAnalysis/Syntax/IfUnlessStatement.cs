using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

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
    public ImmutableArray<ElseClause> ElseClauses { get; internal init; }

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

    public bool IsBareElseClause => IfOrUnlessToken is null;
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
        var elseClauses = ImmutableArray.CreateBuilder<ElseClause>();
        var diagnosticBuilder = new DiagnosticBag.Builder();

        while (Current.Kind == SyntaxKind.ElseKeywordToken)
        {
            var elseClause = ParseElseClause();
            if (elseClause.IfOrUnlessToken.HasValue && elseClause.IfOrUnlessToken.Value.Kind != ifOrUnlessToken.Kind)
            {
                diagnosticBuilder.Add(new Diagnostic()
                {
                    Message = "if/unless qualifier mismatch",
                    ErrorCode = ErrorCode.IfUnlessKeywordMismatch,
                    Level = DiagnosticLevel.Error,
                    TextLocation = elseClause.IfOrUnlessToken.Value.GetTextLocation()
                });
            }

            elseClauses.Add(elseClause);
        }

        if (elseClauses.Any())
        {
            var bareElseClauses = elseClauses.Where(e => !e.IfOrUnlessToken.HasValue);

            if (elseClauses.Last().IfOrUnlessToken.HasValue && bareElseClauses.Any())
            {
                foreach (var b in bareElseClauses)
                {
                    diagnosticBuilder.Add(new Diagnostic()
                    {
                        Message = "bare else clauses must be after all other else clauses",
                        ErrorCode = ErrorCode.MisplacedBareElseClauses,
                        Level = DiagnosticLevel.Error,
                        TextLocation = b.ElseToken.GetTextLocation()
                    });
                }
            }

            if (bareElseClauses.Count() > 1)
            {
                foreach (var b in bareElseClauses)
                {
                    diagnosticBuilder.Add(new Diagnostic()
                    {
                        Message = "duplicate bare else clauses",
                        ErrorCode = ErrorCode.DuplicateBareElseClauses,
                        Level = DiagnosticLevel.Error,
                        TextLocation = b.ElseToken.GetTextLocation()
                    });
                }
            }
        }

        return new()
        {
            SyntaxTree = syntaxTree,
            IfOrUnlessToken = ifOrUnlessToken,
            ConditionExpression = conditionExpression,
            BlockStatement = blockStatement,
            ElseClauses = elseClauses.ToImmutable(),
            DiagnosticBuilder = diagnosticBuilder
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
