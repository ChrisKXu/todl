using System;
using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundConditionalStatement : BoundStatement
{
    public BoundExpression Condition { get; internal init; }
    public BoundStatement Consequence { get; internal init; }
    public BoundStatement Alternative { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundConditionalStatement(this);
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

        BoundStatement current = boundBlockStatement.Statements.Any() ? boundBlockStatement : new BoundNoOpStatement();
        BoundStatement next;

        if (!remaining.IsEmpty)
        {
            next = BindElseClause(remaining[0], remaining.Slice(1));
        }
        else
        {
            next = new BoundNoOpStatement();
        }

        var result = BoundNodeFactory.CreateBoundConditionalStatement(
            syntaxNode: elseClause,
            condition: condition,
            consequence: inverted ? next : current,
            alternative: inverted ? current : next);

        ValidateResultType(result);
        return result;
    }

    private BoundConditionalStatement BindIfUnlessStatement(IfUnlessStatement ifUnlessStatement)
    {
        var inverted = ifUnlessStatement.IfOrUnlessToken.Kind == SyntaxKind.UnlessKeywordToken;
        var condition = BindExpression(ifUnlessStatement.ConditionExpression);
        var boundBlockStatement = BindBlockStatement(ifUnlessStatement.BlockStatement);

        BoundStatement current = boundBlockStatement.Statements.Any() ? boundBlockStatement : new BoundNoOpStatement();
        BoundStatement boundElseClause;

        if (ifUnlessStatement.ElseClauses.Any())
        {
            var elseClauses = ifUnlessStatement.ElseClauses.AsSpan();
            boundElseClause = BindElseClause(elseClauses[0], elseClauses.Slice(1));
        }
        else
        {
            boundElseClause = new BoundNoOpStatement();
        }

        var result = BoundNodeFactory.CreateBoundConditionalStatement(
            syntaxNode: ifUnlessStatement,
            condition: condition,
            consequence: inverted ? boundElseClause : current,
            alternative: inverted ? current : boundElseClause);

        ValidateResultType(result);
        return result;
    }

    private void ValidateResultType(BoundConditionalStatement boundConditionalStatement)
    {
        if (boundConditionalStatement.Condition.ResultType.SpecialType != SpecialType.ClrBoolean)
        {
            var text = boundConditionalStatement.SyntaxNode switch
            {
                IfUnlessStatement ifUnlessStatement => ifUnlessStatement.ConditionExpression.Text,
                ElseClause elseClause => elseClause.ConditionExpression.Text,
                _ => boundConditionalStatement.SyntaxNode.Text
            };

            ReportDiagnostic(new Diagnostic()
            {
                Message = "Condition expressions need to be of boolean type",
                ErrorCode = ErrorCode.TypeMismatch,
                TextLocation = text.GetTextLocation(),
                Level = DiagnosticLevel.Error
            });
        }
    }
}
