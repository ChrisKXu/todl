using System.Collections.Generic;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

/// <summary>
/// BoundTreeWalker walks and observes the BoundTree without altering the nodes
/// </summary>
internal abstract class BoundTreeWalker : BoundTreeVisitor
{
    public override BoundNode VisitBoundAssignmentExpression(BoundAssignmentExpression boundAssignmentExpression)
    {
        Visit(boundAssignmentExpression.Left);
        Visit(boundAssignmentExpression.Right);

        return boundAssignmentExpression;
    }

    public override BoundNode VisitBoundBinaryExpression(BoundBinaryExpression boundBinaryExpression)
    {
        Visit(boundBinaryExpression.Left);
        Visit(boundBinaryExpression.Right);

        return boundBinaryExpression;
    }

    public override BoundNode VisitBoundBlockStatement(BoundBlockStatement boundBlockStatement)
    {
        VisitList(boundBlockStatement.Statements);

        return boundBlockStatement;
    }

    public override BoundNode VisitBoundBreakStatement(BoundBreakStatement boundBreakStatement)
        => boundBreakStatement;

    public override BoundNode VisitBoundClrFieldAccessExpression(BoundClrFieldAccessExpression boundClrFieldAccessExpression)
    {
        Visit(boundClrFieldAccessExpression.BoundBaseExpression);

        return boundClrFieldAccessExpression;
    }

    public override BoundNode VisitBoundClrFunctionCallExpression(BoundClrFunctionCallExpression boundClrFunctionCallExpression)
    {
        Visit(boundClrFunctionCallExpression.BoundBaseExpression);
        VisitList(boundClrFunctionCallExpression.BoundArguments);

        return boundClrFunctionCallExpression;
    }

    public override BoundNode VisitBoundClrPropertyAccessExpression(BoundClrPropertyAccessExpression boundClrPropertyAccessExpression)
    {
        Visit(boundClrPropertyAccessExpression.BoundBaseExpression);

        return boundClrPropertyAccessExpression;
    }

    public override BoundNode VisitBoundConditionalStatement(BoundConditionalStatement boundConditionalStatement)
    {
        Visit(boundConditionalStatement.Condition);
        Visit(boundConditionalStatement.Consequence);
        Visit(boundConditionalStatement.Alternative);

        return boundConditionalStatement;
    }

    public override BoundNode VisitBoundConstant(BoundConstant boundConstant)
        => boundConstant;

    public override BoundNode VisitBoundContinueStatement(BoundContinueStatement boundContinueStatement)
        => boundContinueStatement;

    public override BoundNode VisitBoundEntryPointTypeDefinition(BoundEntryPointTypeDefinition boundEntryPointTypeDefinition)
    {
        VisitList(boundEntryPointTypeDefinition.BoundMembers);

        return boundEntryPointTypeDefinition;
    }

    public override BoundNode VisitBoundExpressionStatement(BoundExpressionStatement boundExpressionStatement)
    {
        Visit(boundExpressionStatement.Expression);

        return boundExpressionStatement;
    }

    public override BoundNode VisitBoundFunctionMember(BoundFunctionMember boundFunctionMember)
    {
        Visit(boundFunctionMember.Body);

        return boundFunctionMember;
    }

    public override BoundNode VisitBoundInvalidMemberAccessExpression(BoundInvalidMemberAccessExpression boundInvalidMemberAccess)
    {
        Visit(boundInvalidMemberAccess.BoundBaseExpression);

        return boundInvalidMemberAccess;
    }

    public override BoundNode VisitBoundLoopStatement(BoundLoopStatement boundLoopStatement)
    {
        Visit(boundLoopStatement.Condition);
        Visit(boundLoopStatement.Body);

        return boundLoopStatement;
    }

    public override BoundNode VisitBoundNoOpStatement(BoundNoOpStatement boundNoOpStatement)
        => boundNoOpStatement;

    public override BoundNode VisitBoundObjectCreationExpression(BoundObjectCreationExpression boundObjectCreationExpression)
    {
        VisitList(boundObjectCreationExpression.BoundArguments);

        return boundObjectCreationExpression;
    }

    public override BoundNode VisitBoundReturnStatement(BoundReturnStatement boundReturnStatement)
    {
        Visit(boundReturnStatement.BoundReturnValueExpression);

        return boundReturnStatement;
    }

    public override BoundNode VisitBoundTodlFunctionCallExpression(BoundTodlFunctionCallExpression boundTodlFunctionCallExpression)
    {
        VisitList(boundTodlFunctionCallExpression.BoundArguments.Values);

        return boundTodlFunctionCallExpression;
    }

    public override BoundNode VisitBoundTypeExpression(BoundTypeExpression boundTypeExpression)
        => boundTypeExpression;

    public override BoundNode VisitBoundUnaryExpression(BoundUnaryExpression boundUnaryExpression)
    {
        Visit(boundUnaryExpression.Operand);

        return boundUnaryExpression;
    }

    public override BoundNode VisitBoundVariableDeclarationStatement(BoundVariableDeclarationStatement boundVariableDeclarationStatement)
    {
        Visit(boundVariableDeclarationStatement.InitializerExpression);

        return boundVariableDeclarationStatement;
    }

    public override BoundNode VisitBoundVariableExpression(BoundVariableExpression boundVariableExpression)
        => boundVariableExpression;

    public override BoundNode VisitBoundVariableMember(BoundVariableMember boundVariableMember)
    {
        Visit(boundVariableMember.BoundVariableDeclarationStatement);

        return boundVariableMember;
    }

    private void VisitList<TBoundNode>(IEnumerable<TBoundNode> list) where TBoundNode : BoundNode
    {
        if (list is null)
        {
            return;
        }

        foreach (var node in list)
        {
            Visit(node);
        }
    }
}
