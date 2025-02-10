using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;

namespace Todl.Compiler.CodeAnalysis.Binding;

internal abstract class BoundTreeRewriter : BoundTreeVisitor
{
    public override BoundNode VisitBoundAssignmentExpression(BoundAssignmentExpression boundAssignmentExpression)
    {
        var left = VisitBoundExpression(boundAssignmentExpression.Left);
        var right = VisitBoundExpression(boundAssignmentExpression.Right);

        return left == boundAssignmentExpression.Left && right == boundAssignmentExpression.Right
            ? boundAssignmentExpression
            : BoundNodeFactory.CreateBoundAssignmentExpression(boundAssignmentExpression.SyntaxNode, left, boundAssignmentExpression.Operator, right);
    }

    public override BoundNode VisitBoundBinaryExpression(BoundBinaryExpression boundBinaryExpression)
    {
        var left = VisitBoundExpression(boundBinaryExpression.Left);
        var right = VisitBoundExpression(boundBinaryExpression.Right);

        return left == boundBinaryExpression.Left && right == boundBinaryExpression.Right
            ? boundBinaryExpression
            : BoundNodeFactory.CreateBoundBinaryExpression(boundBinaryExpression.SyntaxNode, boundBinaryExpression.Operator, left, right);
    }

    public override BoundNode VisitBoundBlockStatement(BoundBlockStatement boundBlockStatement)
    {
        var statements = VisitList(boundBlockStatement.Statements);

        return statements == boundBlockStatement.Statements
            ? boundBlockStatement
            : BoundNodeFactory.CreateBoundBlockStatement(boundBlockStatement.SyntaxNode, boundBlockStatement.Scope, statements);
    }

    public override BoundNode VisitBoundBreakStatement(BoundBreakStatement boundBreakStatement)
        => boundBreakStatement;

    public override BoundNode VisitBoundClrFieldAccessExpression(BoundClrFieldAccessExpression boundClrFieldAccessExpression)
    {
        var baseExpression = VisitBoundExpression(boundClrFieldAccessExpression.BoundBaseExpression);

        return baseExpression == boundClrFieldAccessExpression.BoundBaseExpression
            ? boundClrFieldAccessExpression
            : BoundNodeFactory.CreateBoundClrFieldAccessExpression(boundClrFieldAccessExpression.SyntaxNode, baseExpression, boundClrFieldAccessExpression.FieldInfo);
    }

    public override BoundNode VisitBoundClrFunctionCallExpression(BoundClrFunctionCallExpression boundClrFunctionCallExpression)
    {
        var baseExpression = VisitBoundExpression(boundClrFunctionCallExpression.BoundBaseExpression);
        var arguments = VisitList(boundClrFunctionCallExpression.BoundArguments);

        return baseExpression == boundClrFunctionCallExpression.BoundBaseExpression && arguments == boundClrFunctionCallExpression.BoundArguments
            ? boundClrFunctionCallExpression
            : BoundNodeFactory.CreateBoundClrFunctionCallExpression(boundClrFunctionCallExpression.SyntaxNode, baseExpression, boundClrFunctionCallExpression.MethodInfo, arguments);
    }

    public override BoundNode VisitBoundClrPropertyAccessExpression(BoundClrPropertyAccessExpression boundClrPropertyAccessExpression)
    {
        var baseExpression = VisitBoundExpression(boundClrPropertyAccessExpression.BoundBaseExpression);

        return baseExpression == boundClrPropertyAccessExpression.BoundBaseExpression
            ? boundClrPropertyAccessExpression
            : BoundNodeFactory.CreateBoundClrPropertyAccessExpression(boundClrPropertyAccessExpression.SyntaxNode, baseExpression, boundClrPropertyAccessExpression.PropertyInfo);
    }

    public override BoundNode VisitBoundConditionalStatement(BoundConditionalStatement boundConditionalStatement)
    {
        var condition = VisitBoundExpression(boundConditionalStatement.Condition);
        var consequence = VisitBoundStatement(boundConditionalStatement.Consequence);
        var alternative = VisitBoundStatement(boundConditionalStatement.Alternative);

        return condition == boundConditionalStatement.Condition && consequence == boundConditionalStatement.Consequence && alternative == boundConditionalStatement.Alternative
            ? boundConditionalStatement
            : BoundNodeFactory.CreateBoundConditionalStatement(boundConditionalStatement.SyntaxNode, condition, consequence, alternative);
    }

    public override BoundNode VisitBoundConstant(BoundConstant boundConstant)
        => boundConstant;

    public override BoundNode VisitBoundContinueStatement(BoundContinueStatement boundContinueStatement)
        => boundContinueStatement;

    public override BoundNode VisitBoundEntryPointTypeDefinition(BoundEntryPointTypeDefinition boundEntryPointTypeDefinition)
    {
        var members = VisitList(boundEntryPointTypeDefinition.BoundMembers);
        return members == boundEntryPointTypeDefinition.BoundMembers
            ? boundEntryPointTypeDefinition
            : new BoundEntryPointTypeDefinition()
            {
                SyntaxNode = boundEntryPointTypeDefinition.SyntaxNode,
                BoundMembers = members
            };
    }

    public override BoundNode VisitBoundExpressionStatement(BoundExpressionStatement boundExpressionStatement)
    {
        var expression = VisitBoundExpression(boundExpressionStatement.Expression);

        return expression == boundExpressionStatement.Expression
            ? boundExpressionStatement
            : BoundNodeFactory.CreateBoundExpressionStatement(boundExpressionStatement.SyntaxNode, expression);
    }

    public override BoundNode VisitBoundFunctionMember(BoundFunctionMember boundFunctionMember)
    {
        var body = (BoundBlockStatement)VisitBoundStatement(boundFunctionMember.Body);

        return body == boundFunctionMember.Body
            ? boundFunctionMember
            : BoundNodeFactory.CreateBoundFunctionMember(boundFunctionMember.SyntaxNode, boundFunctionMember.FunctionScope, body, boundFunctionMember.FunctionSymbol);
    }

    public override BoundNode VisitBoundInvalidMemberAccessExpression(BoundInvalidMemberAccessExpression boundInvalidMemberAccess)
    {
        var baseExpression = VisitBoundExpression(boundInvalidMemberAccess.BoundBaseExpression);

        return baseExpression == boundInvalidMemberAccess.BoundBaseExpression
            ? boundInvalidMemberAccess
            : BoundNodeFactory.CreateBoundInvalidMemberAccessExpression(boundInvalidMemberAccess.SyntaxNode, baseExpression);
    }

    public override BoundNode VisitBoundLoopStatement(BoundLoopStatement boundLoopStatement)
    {
        var condition = VisitBoundExpression(boundLoopStatement.Condition);
        var body = VisitBoundStatement(boundLoopStatement.Body);

        return condition == boundLoopStatement.Condition && body == boundLoopStatement.Body
            ? boundLoopStatement
            : BoundNodeFactory.CreateBoundLoopStatement(boundLoopStatement.SyntaxNode, condition, boundLoopStatement.ConditionNegated, body, boundLoopStatement.BoundLoopContext);
    }

    public override BoundNode VisitBoundNoOpStatement(BoundNoOpStatement boundNoOpStatement)
        => boundNoOpStatement;

    public override BoundNode VisitBoundObjectCreationExpression(BoundObjectCreationExpression boundObjectCreationExpression)
    {
        var arguments = VisitList(boundObjectCreationExpression.BoundArguments);

        return arguments == boundObjectCreationExpression.BoundArguments
            ? boundObjectCreationExpression
            : BoundNodeFactory.CreateBoundObjectCreationExpression(boundObjectCreationExpression.SyntaxNode, boundObjectCreationExpression.ConstructorInfo, arguments);
    }

    public override BoundNode VisitBoundReturnStatement(BoundReturnStatement boundReturnStatement)
    {
        var returnValueExpression = VisitBoundExpression(boundReturnStatement.BoundReturnValueExpression);

        return returnValueExpression == boundReturnStatement.BoundReturnValueExpression
            ? boundReturnStatement
            : BoundNodeFactory.CreateBoundReturnStatement(boundReturnStatement.SyntaxNode, returnValueExpression);
    }

    public override BoundNode VisitBoundTodlFunctionCallExpression(BoundTodlFunctionCallExpression boundTodlFunctionCallExpression)
    {
        var arguments = VisitList(boundTodlFunctionCallExpression.BoundArguments);

        return arguments == boundTodlFunctionCallExpression.BoundArguments
            ? boundTodlFunctionCallExpression
            : BoundNodeFactory.CreateBoundTodlFunctionCallExpression(boundTodlFunctionCallExpression.SyntaxNode, boundTodlFunctionCallExpression.FunctionSymbol, arguments);
    }

    public override BoundNode VisitBoundTypeExpression(BoundTypeExpression boundTypeExpression)
        => boundTypeExpression;

    public override BoundNode VisitBoundUnaryExpression(BoundUnaryExpression boundUnaryExpression)
    {
        var operand = VisitBoundExpression(boundUnaryExpression.Operand);

        return operand == boundUnaryExpression.Operand
            ? boundUnaryExpression
            : BoundNodeFactory.CreateBoundUnaryExpression(boundUnaryExpression.SyntaxNode, boundUnaryExpression.Operator, operand);
    }

    public override BoundNode VisitBoundVariableDeclarationStatement(BoundVariableDeclarationStatement boundVariableDeclarationStatement)
    {
        var initializerExpression = VisitBoundExpression(boundVariableDeclarationStatement.InitializerExpression);

        return initializerExpression == boundVariableDeclarationStatement.InitializerExpression
            ? boundVariableDeclarationStatement
            : BoundNodeFactory.CreateBoundVariableDeclarationStatement(boundVariableDeclarationStatement.SyntaxNode, boundVariableDeclarationStatement.Variable, initializerExpression);
    }

    public override BoundNode VisitBoundVariableExpression(BoundVariableExpression boundVariableExpression)
        => boundVariableExpression;

    public override BoundNode VisitBoundVariableMember(BoundVariableMember boundVariableMember)
    {
        var variableDeclarationStatement = (BoundVariableDeclarationStatement)VisitBoundVariableDeclarationStatement(boundVariableMember.BoundVariableDeclarationStatement);

        return variableDeclarationStatement == boundVariableMember.BoundVariableDeclarationStatement
            ? boundVariableMember
            : BoundNodeFactory.CreateBoundVariableMember(boundVariableMember.SyntaxNode, variableDeclarationStatement);
    }

    protected BoundExpression VisitBoundExpression(BoundExpression boundExpression)
        => (BoundExpression)Visit(boundExpression);

    protected BoundStatement VisitBoundStatement(BoundStatement boundStatement)
        => (BoundStatement)Visit(boundStatement);

    protected ImmutableArray<TBoundNode> VisitList<TBoundNode>(ImmutableArray<TBoundNode> nodes)
        where TBoundNode : BoundNode
    {
        if (nodes.IsDefaultOrEmpty)
        {
            return nodes;
        }

        var builder = ImmutableArray.CreateBuilder<TBoundNode>(nodes.Length);
        var modified = false;

        foreach (var node in nodes)
        {
            var newNode = (TBoundNode)Visit(node);
            modified |= newNode != node;
            if (newNode is not null)
            {
                builder.Add(newNode);
            }
        }

        return modified ? builder.ToImmutable() : nodes;
    }

    protected ImmutableDictionary<TKey, TBoundNode> VisitList<TKey, TBoundNode>(ImmutableDictionary<TKey, TBoundNode> nodes)
        where TBoundNode : BoundNode
    {
        if (nodes is null || nodes.IsEmpty)
        {
            return nodes;
        }

        var builder = ImmutableDictionary.CreateBuilder<TKey, TBoundNode>();
        var modified = false;

        foreach (var (key, node) in nodes)
        {
            var newNode = (TBoundNode)Visit(node);
            modified |= newNode != node;
            if (newNode is not null)
            {
                builder.Add(key, newNode);
            }
        }

        return modified ? builder.ToImmutable() : nodes;
    }
}
