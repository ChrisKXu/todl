using System;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundConditionalStatement : BoundStatement
{
    public BoundExpression Condition { get; internal init; }
    public BoundStatement Consequence { get; internal init; }
    public BoundStatement Alternative { get; internal init; }

    public BoundConditionalStatement Validate()
    {
        if (!Condition.ResultType.Equals(SyntaxNode.SyntaxTree.ClrTypeCache.BuiltInTypes.Boolean))
        {
            var text = SyntaxNode switch
            {
                IfUnlessStatement ifUnlessStatement => ifUnlessStatement.ConditionExpression.Text,
                ElseClause elseClause => elseClause.ConditionExpression.Text,
                _ => SyntaxNode.Text
            };

            DiagnosticBuilder.Add(new Diagnostic()
            {
                Message = "Condition expressions need to be of boolean type",
                ErrorCode = ErrorCode.TypeMismatch,
                TextLocation = text.GetTextLocation(),
                Level = DiagnosticLevel.Error
            });
        }

        return this;
    }
}

public partial class Binder
{
    private BoundStatement BindElseClause(ElseClause elseClause, ReadOnlySpan<ElseClause> remaining)
    {
        if (elseClause.IsBareElseClause)
        {
            return BindBlockStatement(elseClause.BlockStatement);
        }

        var inverted = elseClause.IfOrUnlessToken.Value.Kind == SyntaxKind.UnlessKeywordToken;
        var condition = BindExpression(elseClause.ConditionExpression);
        var boundBlockStatement = BindBlockStatement(elseClause.BlockStatement);
        BoundStatement next;

        if (!remaining.IsEmpty)
        {
            next = BindElseClause(remaining[0], remaining.Slice(1));
        }
        else
        {
            next = new BoundNoOpStatement();
        }

        return BoundNodeFactory.CreateBoundConditionalStatement(
            syntaxNode: elseClause,
            condition: condition,
            consequence: inverted ? next : boundBlockStatement,
            alternative: inverted ? boundBlockStatement : next).Validate();
    }

    private BoundConditionalStatement BindIfUnlessStatement(IfUnlessStatement ifUnlessStatement)
    {
        var inverted = ifUnlessStatement.IfOrUnlessToken.Kind == SyntaxKind.UnlessKeywordToken;
        var condition = BindExpression(ifUnlessStatement.ConditionExpression);
        var boundBlockStatement = BindBlockStatement(ifUnlessStatement.BlockStatement);
        BoundStatement boundElseClause;

        if (ifUnlessStatement.ElseClauses.Any())
        {
            var elseClauses = new ReadOnlySpan<ElseClause>(ifUnlessStatement.ElseClauses.ToArray());
            boundElseClause = BindElseClause(elseClauses[0], elseClauses.Slice(1));
        }
        else
        {
            boundElseClause = new BoundNoOpStatement();
        }

        return BoundNodeFactory.CreateBoundConditionalStatement(
            syntaxNode: ifUnlessStatement,
            condition: condition,
            consequence: inverted ? boundElseClause : boundBlockStatement,
            alternative: inverted ? boundBlockStatement : boundElseClause).Validate();
    }
}
