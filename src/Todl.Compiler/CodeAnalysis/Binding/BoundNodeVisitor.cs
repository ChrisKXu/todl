using System;
using System.Collections.Generic;
using System.Linq;

namespace Todl.Compiler.CodeAnalysis.Binding;

internal abstract partial class BoundNodeVisitor
{
    public virtual BoundTodlTypeDefinition VisitBoundTypeDefinition(BoundTodlTypeDefinition boundTodlTypeDefinition)
    {
        if (boundTodlTypeDefinition is BoundEntryPointTypeDefinition)
        {
            var boundMembers = VisitBoundMembers(boundTodlTypeDefinition.BoundMembers).ToList();
            boundTodlTypeDefinition.DiagnosticBuilder.AddRange(boundMembers);

            // TODO: Use BoundNodeFactory to initialize BoundEntryPointTypeDefinition
            return new BoundEntryPointTypeDefinition()
            {
                SyntaxNode = boundTodlTypeDefinition.SyntaxNode,
                BoundMembers = boundMembers,
                DiagnosticBuilder = boundTodlTypeDefinition.DiagnosticBuilder
            };
        }

        throw new NotSupportedException();
    }

    public virtual BoundExpression VisitBoundExpression(BoundExpression boundExpression)
        => boundExpression switch
        {
            BoundAssignmentExpression boundAssignmentExpression => VisitBoundAssignmentExpression(boundAssignmentExpression),
            BoundBinaryExpression boundBinaryExpression => VisitBoundBinaryExpression(boundBinaryExpression),
            BoundConstant boundConstant => VisitBoundConstant(boundConstant),
            BoundClrFunctionCallExpression boundClrFunctionCallExpression => VisitBoundClrFunctionCallExpression(boundClrFunctionCallExpression),
            BoundMemberAccessExpression boundMemberAccessExpression => VisitBoundMemberAccessExpression(boundMemberAccessExpression),
            BoundObjectCreationExpression boundObjectCreationExpression => VisitBoundObjectCreationExpression(boundObjectCreationExpression),
            BoundTodlFunctionCallExpression boundTodlFunctionCallExpression => VisitBoundTodlFunctionCallExpression(boundTodlFunctionCallExpression),
            BoundTypeExpression boundTypeExpression => VisitBoundTypeExpression(boundTypeExpression),
            BoundUnaryExpression boundUnaryExpression => VisitBoundUnaryExpression(boundUnaryExpression),
            BoundVariableExpression boundVariableExpression => VisitBoundVariableExpression(boundVariableExpression),
            _ => throw new NotSupportedException()
        };

    public virtual BoundMember VisitBoundMember(BoundMember boundMember)
        => boundMember switch
        {
            BoundFunctionMember boundFunctionMember => VisitBoundFunctionMember(boundFunctionMember),
            BoundVariableMember boundVariableMember => VisitBoundVariableMember(boundVariableMember),
            _ => throw new NotSupportedException()
        };

    public virtual BoundStatement VisitBoundStatement(BoundStatement boundStatement)
        => boundStatement switch
        {
            BoundBlockStatement boundBlockStatement => VisitBoundBlockStatement(boundBlockStatement),
            BoundExpressionStatement boundExpressionStatement => VisitBoundExpressionStatement(boundExpressionStatement),
            BoundReturnStatement boundReturnStatement => VisitBoundReturnStatement(boundReturnStatement),
            BoundVariableDeclarationStatement boundVariableDeclarationStatement => VisitBoundVariableDeclarationStatement(boundVariableDeclarationStatement),
            _ => throw new NotSupportedException()
        };

    public virtual IEnumerable<BoundMember> VisitBoundMembers(IEnumerable<BoundMember> boundMembers)
        => boundMembers.Select(m => VisitBoundMember(m));

    protected virtual BoundExpression VisitBoundAssignmentExpression(BoundAssignmentExpression boundAssignmentExpression)
    {
        var newLeft = VisitBoundExpression(boundAssignmentExpression.Left);
        var newRight = VisitBoundExpression(boundAssignmentExpression.Right);
        if (newLeft == boundAssignmentExpression.Left && newRight == boundAssignmentExpression.Right)
        {
            return boundAssignmentExpression;
        }

        return BoundNodeFactory.CreateBoundAssignmentExpression(
            syntaxNode: boundAssignmentExpression.SyntaxNode,
            left: newLeft,
            @operator: boundAssignmentExpression.Operator,
            right: newRight,
            diagnosticBuilder: boundAssignmentExpression.DiagnosticBuilder);
    }

    protected virtual BoundExpression VisitBoundBinaryExpression(BoundBinaryExpression boundBinaryExpression)
    {
        var newLeft = VisitBoundExpression(boundBinaryExpression.Left);
        var newRight = VisitBoundExpression(boundBinaryExpression.Right);

        if (newLeft == boundBinaryExpression.Left && newRight == boundBinaryExpression.Right)
        {
            return boundBinaryExpression;
        }

        return BoundNodeFactory.CreateBoundBinaryExpression(
            syntaxNode: boundBinaryExpression.SyntaxNode,
            @operator: boundBinaryExpression.Operator,
            left: newLeft,
            right: newRight,
            diagnosticBuilder: boundBinaryExpression.DiagnosticBuilder);
    }

    protected virtual BoundExpression VisitBoundConstant(BoundConstant boundConstant)
        => boundConstant;

    protected virtual BoundExpression VisitBoundClrFunctionCallExpression(BoundClrFunctionCallExpression boundClrFunctionCallExpression)
        => boundClrFunctionCallExpression;

    protected virtual BoundExpression VisitBoundMemberAccessExpression(BoundMemberAccessExpression boundMemberAccessExpression)
        => boundMemberAccessExpression;

    protected virtual BoundExpression VisitBoundObjectCreationExpression(BoundObjectCreationExpression boundObjectCreationExpression)
        => boundObjectCreationExpression;

    protected virtual BoundExpression VisitBoundTodlFunctionCallExpression(BoundTodlFunctionCallExpression boundTodlFunctionCallExpression)
        => boundTodlFunctionCallExpression;

    protected virtual BoundExpression VisitBoundTypeExpression(BoundTypeExpression boundTypeExpression)
        => boundTypeExpression;

    protected virtual BoundExpression VisitBoundUnaryExpression(BoundUnaryExpression boundUnaryExpression)
    {
        var newOperand = VisitBoundExpression(boundUnaryExpression.Operand);
        if (newOperand == boundUnaryExpression.Operand)
        {
            return boundUnaryExpression;
        }

        return BoundNodeFactory.CreateBoundUnaryExpression(
            syntaxNode: boundUnaryExpression.SyntaxNode,
            @operator: boundUnaryExpression.Operator,
            operand: newOperand,
            diagnosticBuilder: boundUnaryExpression.DiagnosticBuilder);
    }

    protected virtual BoundExpression VisitBoundVariableExpression(BoundVariableExpression boundVariableExpression)
        => boundVariableExpression;

    protected virtual BoundMember VisitBoundFunctionMember(BoundFunctionMember boundFunctionMember)
    {
        var newBody = VisitBoundBlockStatement(boundFunctionMember.Body);
        if (newBody == boundFunctionMember.Body)
        {
            return boundFunctionMember;
        }

        return BoundNodeFactory.CreateBoundFunctionMember(
            syntaxNode: boundFunctionMember.SyntaxNode,
            functionScope: boundFunctionMember.FunctionScope,
            body: (BoundBlockStatement)newBody,
            functionSymbol: boundFunctionMember.FunctionSymbol,
            diagnosticBuilder: boundFunctionMember.DiagnosticBuilder);
    }

    protected virtual BoundVariableMember VisitBoundVariableMember(BoundVariableMember boundVariableMember)
    {
        var newStatement = VisitBoundVariableDeclarationStatement(boundVariableMember.BoundVariableDeclarationStatement);
        if (newStatement == boundVariableMember.BoundVariableDeclarationStatement)
        {
            return boundVariableMember;
        }

        return BoundNodeFactory.CreateBoundVariableMember(
            syntaxNode: boundVariableMember.SyntaxNode,
            boundVariableDeclarationStatement: (BoundVariableDeclarationStatement)newStatement,
            diagnosticBuilder: boundVariableMember.DiagnosticBuilder);
    }

    protected virtual BoundStatement VisitBoundBlockStatement(BoundBlockStatement boundBlockStatement)
    {
        var hasChange = false;
        var statements = boundBlockStatement.Statements.Select(boundStatement =>
        {
            var newBoundStatement = VisitBoundStatement(boundStatement);
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

    protected virtual BoundStatement VisitBoundExpressionStatement(BoundExpressionStatement boundExpressionStatement)
    {
        var newExpression = VisitBoundExpression(boundExpressionStatement.Expression);
        if (newExpression == boundExpressionStatement.Expression)
        {
            return boundExpressionStatement;
        }

        return BoundNodeFactory.CreateBoundExpressionStatement(
            syntaxNode: boundExpressionStatement.SyntaxNode,
            expression: newExpression,
            diagnosticBuilder: boundExpressionStatement.DiagnosticBuilder);
    }

    protected virtual BoundStatement VisitBoundReturnStatement(BoundReturnStatement boundReturnStatement)
    {
        if (boundReturnStatement.BoundReturnValueExpression is null)
        {
            return boundReturnStatement;
        }

        var newExpression = VisitBoundExpression(boundReturnStatement.BoundReturnValueExpression);
        if (newExpression == boundReturnStatement.BoundReturnValueExpression)
        {
            return boundReturnStatement;
        }

        return BoundNodeFactory.CreateBoundReturnStatement(
            syntaxNode: boundReturnStatement.SyntaxNode,
            boundReturnValueExpression: newExpression,
            diagnosticBuilder: boundReturnStatement.DiagnosticBuilder);
    }

    protected virtual BoundStatement VisitBoundVariableDeclarationStatement(BoundVariableDeclarationStatement boundVariableDeclarationStatement)
        => boundVariableDeclarationStatement;
}
