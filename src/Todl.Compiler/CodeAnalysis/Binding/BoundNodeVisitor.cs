using System;
using System.Linq;

namespace Todl.Compiler.CodeAnalysis.Binding;

internal abstract partial class BoundNodeVisitor
{
    public virtual BoundExpression VisitExpression(BoundExpression boundExpression)
    {
        return boundExpression;
    }

    public virtual BoundStatement VisitStatement(BoundStatement boundStatement)
        => boundStatement switch
        {
            BoundBlockStatement boundBlockStatement => VisitBlockStatement(boundBlockStatement),
            _ => throw new NotSupportedException()
        };

    public virtual BoundMember VisitMember(BoundMember boundMember)
        => boundMember switch
        {
            BoundFunctionMember boundFunctionMember => VisitFunctionMember(boundFunctionMember),
            _ => throw new NotSupportedException()
        };

    protected virtual BoundStatement VisitBlockStatement(BoundBlockStatement boundBlockStatement)
    {
        var hasChange = false;
        var statements = boundBlockStatement.Statements.Select(boundStatement =>
        {
            var newBoundStatement = VisitStatement(boundStatement);
            if (newBoundStatement != boundStatement)
            {
                hasChange = true;
            }
            return newBoundStatement;
        });

        if (!hasChange)
        {
            return boundBlockStatement;
        }

        return BoundNodeFactory.CreateBoundBlockStatement(
            syntaxNode: boundBlockStatement.SyntaxNode,
            scope: boundBlockStatement.Scope,
            statements: statements.ToList(),
            diagnosticBuilder: boundBlockStatement.DiagnosticBuilder);
    }

    protected virtual BoundMember VisitFunctionMember(BoundFunctionMember boundMember)
    {
        var newBody = VisitBlockStatement(boundMember.Body);
        if (newBody == boundMember.Body)
        {
            return boundMember;
        }

        return BoundNodeFactory.CreateBoundFunctionMember(
            syntaxNode: boundMember.SyntaxNode,
            functionScope: boundMember.FunctionScope,
            body: (BoundBlockStatement)newBody,
            functionSymbol: boundMember.FunctionSymbol,
            diagnosticBuilder: boundMember.DiagnosticBuilder);
    }
}
