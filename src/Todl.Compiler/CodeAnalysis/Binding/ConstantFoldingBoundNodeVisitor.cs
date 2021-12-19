using System;
using System.Collections.Generic;
using System.Diagnostics;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding;

internal sealed class ConstantFoldingBoundNodeVisitor : BoundNodeVisitor
{
    private readonly Dictionary<VariableSymbol, BoundConstant> constantMap = new();

    public override BoundExpression VisitBoundExpression(BoundExpression boundExpression)
    {
        if (!boundExpression.Constant)
        {
            return boundExpression;
        }

        return base.VisitBoundExpression(boundExpression);
    }

    protected override BoundExpression VisitBoundBinaryExpression(BoundBinaryExpression boundBinaryExpression)
    {
        var left = ((BoundConstant)VisitBoundExpression(boundBinaryExpression.Left)).Value;
        var right = ((BoundConstant)VisitBoundExpression(boundBinaryExpression.Right)).Value;

        object value = boundBinaryExpression.Operator.BoundBinaryOperatorKind switch
        {
            BoundBinaryExpression.BoundBinaryOperatorKind.NumericAddition
                => (left, right) switch
                {
                    (int a, int b) => a + b,
                    (long a, long b) => a + b,
                    (double a, double b) => a + b,
                    _ => null
                },
            BoundBinaryExpression.BoundBinaryOperatorKind.NumericSubstraction
                => (left, right) switch
                {
                    (int a, int b) => a - b,
                    (long a, long b) => a - b,
                    (double a, double b) => a - b,
                    _ => null
                },
            BoundBinaryExpression.BoundBinaryOperatorKind.NumericMultiplication
                => (left, right) switch
                {
                    (int a, int b) => a * b,
                    (long a, long b) => a * b,
                    (double a, double b) => a * b,
                    _ => null
                },
            BoundBinaryExpression.BoundBinaryOperatorKind.NumericDivision
                => (left, right) switch
                {
                    (int a, int b) => a / b,
                    (long a, long b) => a / b,
                    (double a, double b) => a / b,
                    _ => null
                },
            BoundBinaryExpression.BoundBinaryOperatorKind.LogicalAnd
                => (left, right) switch
                {
                    (bool a, bool b) => a && b,
                    _ => null
                },
            BoundBinaryExpression.BoundBinaryOperatorKind.LogicalOr
                => (left, right) switch
                {
                    (bool a, bool b) => a || b,
                    _ => null
                },
            BoundBinaryExpression.BoundBinaryOperatorKind.StringConcatenation
                => (left, right) switch
                {
                    (string a, string b) => a + b,
                    _ => null
                },
            _ => null
        };

        if (value is null)
        {
            return boundBinaryExpression;
        }

        return BoundNodeFactory.CreateBoundConstant(
            syntaxNode: boundBinaryExpression.SyntaxNode,
            value: value,
            diagnosticBuilder: boundBinaryExpression.DiagnosticBuilder);
    }

    protected override BoundExpression VisitBoundUnaryExpression(BoundUnaryExpression boundUnaryExpression)
    {
        var constant = (BoundConstant)VisitBoundExpression(boundUnaryExpression.Operand);
        var value = boundUnaryExpression.Operator.BoundUnaryOperatorKind switch
        {
            BoundUnaryExpression.BoundUnaryOperatorKind.Identity => constant.Value,
            BoundUnaryExpression.BoundUnaryOperatorKind.Negation
                => constant.Value switch
                {
                    int intValue => -intValue,
                    long longValue => -longValue,
                    double doubleValue => -doubleValue,
                    _ => null
                },
            BoundUnaryExpression.BoundUnaryOperatorKind.LogicalNegation => !(bool)constant.Value,
            _ => null
        };

        if (value is null)
        {
            return boundUnaryExpression;
        }

        return BoundNodeFactory.CreateBoundConstant(
            syntaxNode: boundUnaryExpression.SyntaxNode,
            value: value,
            diagnosticBuilder: boundUnaryExpression.DiagnosticBuilder);
    }

    protected override BoundStatement VisitBoundVariableDeclarationStatement(BoundVariableDeclarationStatement boundVariableDeclarationStatement)
    {
        if (!boundVariableDeclarationStatement.Variable.Constant)
        {
            return boundVariableDeclarationStatement;
        }

        var constant = (BoundConstant)VisitBoundExpression(boundVariableDeclarationStatement.InitializerExpression);
        constantMap.Add(boundVariableDeclarationStatement.Variable, constant);

        return BoundNodeFactory.CreateBoundVariableDeclarationStatement(
            syntaxNode: boundVariableDeclarationStatement.SyntaxNode,
            variable: boundVariableDeclarationStatement.Variable,
            initializerExpression: constant,
            diagnosticBuilder: boundVariableDeclarationStatement.DiagnosticBuilder);
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
