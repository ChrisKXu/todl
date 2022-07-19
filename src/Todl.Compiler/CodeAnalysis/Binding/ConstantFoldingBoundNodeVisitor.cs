using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding;

internal sealed class ConstantFoldingBoundNodeVisitor : BoundNodeVisitor
{
    private readonly Dictionary<VariableSymbol, BoundConstant> constantMap = new();

    protected override BoundExpression VisitBoundBinaryExpression(BoundBinaryExpression boundBinaryExpression)
    {
        var left = VisitBoundExpression(boundBinaryExpression.Left);
        var right = VisitBoundExpression(boundBinaryExpression.Right);

        // if both of left and right are now constant, fold the binary expression into constant as well
        if (left is BoundConstant constantLeft
            && right is BoundConstant constantRight)
        {
            return FoldBinaryConstant(constantLeft, constantRight, boundBinaryExpression);
        }

        // if there are no changes to both left and right, return the original binary expression
        if (left == boundBinaryExpression.Left && right == boundBinaryExpression.Right)
        {
            return boundBinaryExpression;
        }

        return BoundNodeFactory.CreateBoundBinaryExpression(
            syntaxNode: boundBinaryExpression.SyntaxNode,
            @operator: boundBinaryExpression.Operator,
            left: left,
            right: right,
            diagnosticBuilder: boundBinaryExpression.DiagnosticBuilder);
    }

    private static BoundConstant FoldBinaryConstant(BoundConstant left, BoundConstant right, BoundBinaryExpression boundBinaryExpression)
    {
        var l = left.Value;
        var r = right.Value;

        object value = boundBinaryExpression.Operator.BoundBinaryOperatorKind switch
        {
            BoundBinaryOperatorKind.NumericAddition
                => (l, r) switch
                {
                    (int a, int b) => a + b,
                    (long a, long b) => a + b,
                    (double a, double b) => a + b,
                    _ => null
                },
            BoundBinaryOperatorKind.NumericSubstraction
                => (l, r) switch
                {
                    (int a, int b) => a - b,
                    (long a, long b) => a - b,
                    (double a, double b) => a - b,
                    _ => null
                },
            BoundBinaryOperatorKind.NumericMultiplication
                => (l, r) switch
                {
                    (int a, int b) => a * b,
                    (long a, long b) => a * b,
                    (double a, double b) => a * b,
                    _ => null
                },
            BoundBinaryOperatorKind.NumericDivision
                => (l, r) switch
                {
                    (int a, int b) => a / b,
                    (long a, long b) => a / b,
                    (double a, double b) => a / b,
                    _ => null
                },
            BoundBinaryOperatorKind.LogicalAnd
                => (l, r) switch
                {
                    (bool a, bool b) => a && b,
                    _ => null
                },
            BoundBinaryOperatorKind.LogicalOr
                => (l, r) switch
                {
                    (bool a, bool b) => a || b,
                    _ => null
                },
            BoundBinaryOperatorKind.StringConcatenation
                => (l, r) switch
                {
                    (string a, string b) => a + b,
                    _ => null
                },
            _ => null
        };

        return BoundNodeFactory.CreateBoundConstant(
            syntaxNode: boundBinaryExpression.SyntaxNode,
            value: value,
            diagnosticBuilder: boundBinaryExpression.DiagnosticBuilder);
    }

    protected override BoundExpression VisitBoundUnaryExpression(BoundUnaryExpression boundUnaryExpression)
    {
        var visitedOperand = VisitBoundExpression(boundUnaryExpression.Operand);

        if (visitedOperand is BoundConstant constant)
        {
            var value = boundUnaryExpression.Operator.BoundUnaryOperatorKind switch
            {
                BoundUnaryOperatorKind.Identity => constant.Value,
                BoundUnaryOperatorKind.Negation
                    => constant.Value switch
                    {
                        int intValue => -intValue,
                        long longValue => -longValue,
                        double doubleValue => -doubleValue,
                        _ => null
                    },
                BoundUnaryOperatorKind.LogicalNegation => !(bool)constant.Value,
                _ => null
            };

            return BoundNodeFactory.CreateBoundConstant(
                syntaxNode: boundUnaryExpression.SyntaxNode,
                value: value,
                diagnosticBuilder: boundUnaryExpression.DiagnosticBuilder);
        };

        if (visitedOperand == boundUnaryExpression.Operand)
        {
            return boundUnaryExpression;
        }

        return BoundNodeFactory.CreateBoundUnaryExpression(
            syntaxNode: boundUnaryExpression.SyntaxNode,
            @operator: boundUnaryExpression.Operator,
            operand: visitedOperand,
            diagnosticBuilder: boundUnaryExpression.DiagnosticBuilder);
    }

    protected override BoundStatement VisitBoundVariableDeclarationStatement(BoundVariableDeclarationStatement boundVariableDeclarationStatement)
    {
        var visitedExpression = VisitBoundExpression(boundVariableDeclarationStatement.InitializerExpression);

        if (visitedExpression is BoundConstant constant)
        {
            constantMap.Add(boundVariableDeclarationStatement.Variable, constant);

            if (visitedExpression == boundVariableDeclarationStatement.InitializerExpression)
            {
                return boundVariableDeclarationStatement;
            }

            return BoundNodeFactory.CreateBoundVariableDeclarationStatement(
                syntaxNode: boundVariableDeclarationStatement.SyntaxNode,
                variable: boundVariableDeclarationStatement.Variable,
                initializerExpression: constant,
                diagnosticBuilder: boundVariableDeclarationStatement.DiagnosticBuilder);
        }

        if (visitedExpression == boundVariableDeclarationStatement.InitializerExpression)
        {
            return boundVariableDeclarationStatement;
        }

        return BoundNodeFactory.CreateBoundVariableDeclarationStatement(
            syntaxNode: boundVariableDeclarationStatement.SyntaxNode,
            variable: boundVariableDeclarationStatement.Variable,
            initializerExpression: visitedExpression,
            diagnosticBuilder: boundVariableDeclarationStatement.DiagnosticBuilder);
    }

    protected override BoundStatement VisitBoundReturnStatement(BoundReturnStatement boundReturnStatement)
    {
        if (boundReturnStatement.BoundReturnValueExpression is null)
        {
            return boundReturnStatement;
        }

        var boundReturnValueExpression = VisitBoundExpression(boundReturnStatement.BoundReturnValueExpression);
        if (boundReturnValueExpression == boundReturnStatement.BoundReturnValueExpression)
        {
            return boundReturnStatement;
        }

        return BoundNodeFactory.CreateBoundReturnStatement(
            syntaxNode: boundReturnStatement.SyntaxNode,
            boundReturnValueExpression: boundReturnValueExpression,
            diagnosticBuilder: boundReturnStatement.DiagnosticBuilder);
    }

    protected override BoundExpression VisitBoundVariableExpression(BoundVariableExpression boundVariableExpression)
    {
        if (boundVariableExpression.Constant)
        {
            return constantMap[boundVariableExpression.Variable];
        }

        return boundVariableExpression;
    }
}
