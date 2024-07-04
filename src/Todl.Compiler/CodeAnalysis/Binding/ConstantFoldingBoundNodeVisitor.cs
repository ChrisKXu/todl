using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding;

internal sealed class ConstantFoldingBoundNodeVisitor : BoundNodeVisitor
{
    private readonly Dictionary<VariableSymbol, BoundConstant> constantMap = new();
    private readonly ConstantValueFactory constantValueFactory;

    public ConstantFoldingBoundNodeVisitor(ConstantValueFactory constantValueFactory)
    {
        this.constantValueFactory = constantValueFactory;
    }

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

    private BoundExpression FoldBinaryConstant(BoundConstant left, BoundConstant right, BoundBinaryExpression boundBinaryExpression)
    {
        var l = left.Value;
        var r = right.Value;

        ConstantValue value = boundBinaryExpression.Operator.BoundBinaryOperatorKind switch
        {
            BoundBinaryOperatorKind.NumericAddition
                => (l, r) switch
                {
                    (ConstantInt32Value a, ConstantInt32Value b) => constantValueFactory.Create(a.Int32Value + b.Int32Value),
                    (ConstantInt64Value a, ConstantInt64Value b) => constantValueFactory.Create(a.Int64Value + b.Int64Value),
                    (ConstantDoubleValue a, ConstantDoubleValue b) => constantValueFactory.Create(a.DoubleValue + b.DoubleValue),
                    _ => null
                },
            BoundBinaryOperatorKind.NumericSubstraction
                => (l, r) switch
                {
                    (ConstantInt32Value a, ConstantInt32Value b) => constantValueFactory.Create(a.Int32Value - b.Int32Value),
                    (ConstantInt64Value a, ConstantInt64Value b) => constantValueFactory.Create(a.Int64Value - b.Int64Value),
                    (ConstantDoubleValue a, ConstantDoubleValue b) => constantValueFactory.Create(a.DoubleValue - b.DoubleValue),
                    _ => null
                },
            BoundBinaryOperatorKind.NumericMultiplication
                => (l, r) switch
                {
                    (ConstantInt32Value a, ConstantInt32Value b) => constantValueFactory.Create(a.Int32Value * b.Int32Value),
                    (ConstantInt64Value a, ConstantInt64Value b) => constantValueFactory.Create(a.Int64Value * b.Int64Value),
                    (ConstantDoubleValue a, ConstantDoubleValue b) => constantValueFactory.Create(a.DoubleValue * b.DoubleValue),
                    _ => null
                },
            BoundBinaryOperatorKind.NumericDivision
                => (l, r) switch
                {
                    (ConstantInt32Value a, ConstantInt32Value b) => constantValueFactory.Create(a.Int32Value / b.Int32Value),
                    (ConstantInt64Value a, ConstantInt64Value b) => constantValueFactory.Create(a.Int64Value / b.Int64Value),
                    (ConstantDoubleValue a, ConstantDoubleValue b) => constantValueFactory.Create(a.DoubleValue / b.DoubleValue),
                    _ => null
                },
            BoundBinaryOperatorKind.LogicalAnd
                => (l, r) switch
                {
                    (ConstantBooleanValue a, ConstantBooleanValue b) => constantValueFactory.Create(a.BooleanValue && b.BooleanValue),
                    _ => null
                },
            BoundBinaryOperatorKind.LogicalOr
                => (l, r) switch
                {
                    (ConstantBooleanValue a, ConstantBooleanValue b) => constantValueFactory.Create(a.BooleanValue || b.BooleanValue),
                    _ => null
                },
            BoundBinaryOperatorKind.StringConcatenation
                => (l, r) switch
                {
                    (ConstantStringValue a, ConstantStringValue b) => constantValueFactory.Create(a.StringValue + b.StringValue),
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
        var visitedOperand = VisitBoundExpression(boundUnaryExpression.Operand);

        if (visitedOperand is BoundConstant constant)
        {
            var value = boundUnaryExpression.Operator.BoundUnaryOperatorKind.GetOperationKind() switch
            {
                BoundUnaryOperatorKind.UnaryPlus => constant.Value,
                BoundUnaryOperatorKind.UnaryMinus
                    => constant.ResultType.SpecialType switch
                    {
                        SpecialType.ClrInt32 => constantValueFactory.Create(-constant.Value.Int32Value),
                        SpecialType.ClrUInt32 => constantValueFactory.Create(-constant.Value.UInt32Value),
                        SpecialType.ClrInt64 => constantValueFactory.Create(-constant.Value.Int64Value),
                        SpecialType.ClrFloat => constantValueFactory.Create(-constant.Value.FloatValue),
                        SpecialType.ClrDouble => constantValueFactory.Create(-constant.Value.DoubleValue),
                        _ => null
                    },
                BoundUnaryOperatorKind.LogicalNegation
                    => constant.ResultType.SpecialType switch
                    {
                        SpecialType.ClrBoolean => constantValueFactory.Create(!constant.Value.BooleanValue),
                        _ => null
                    },
                BoundUnaryOperatorKind.BitwiseComplement
                    => constant.ResultType.SpecialType switch
                    {
                        SpecialType.ClrInt32 => constantValueFactory.Create(~constant.Value.Int32Value),
                        SpecialType.ClrUInt32 => constantValueFactory.Create(~constant.Value.UInt32Value),
                        SpecialType.ClrInt64 => constantValueFactory.Create(~constant.Value.Int64Value),
                        SpecialType.ClrUInt64 => constantValueFactory.Create(~constant.Value.UInt64Value),
                        _ => null
                    },
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
