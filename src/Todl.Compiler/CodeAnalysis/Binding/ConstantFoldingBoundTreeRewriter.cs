using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding;

internal sealed class ConstantFoldingBoundTreeRewriter : BoundTreeRewriter
{
    private readonly Dictionary<VariableSymbol, BoundConstant> constantMap = new();
    private readonly ConstantValueFactory constantValueFactory;

    public ConstantFoldingBoundTreeRewriter(ConstantValueFactory constantValueFactory)
    {
        this.constantValueFactory = constantValueFactory;
    }

    public override BoundNode VisitBoundConstant(BoundConstant boundConstant)
        => boundConstant;

    public override BoundNode VisitBoundBinaryExpression(BoundBinaryExpression boundBinaryExpression)
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
            resultType: boundBinaryExpression.ResultType);
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
            resultType: value.ResultType);
    }

    public override BoundNode VisitBoundUnaryExpression(BoundUnaryExpression boundUnaryExpression)
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
                resultType: value.ResultType);
        }

        if (visitedOperand == boundUnaryExpression.Operand)
        {
            return boundUnaryExpression;
        }

        return BoundNodeFactory.CreateBoundUnaryExpression(
            syntaxNode: boundUnaryExpression.SyntaxNode,
            @operator: boundUnaryExpression.Operator,
            operand: visitedOperand,
            resultType: boundUnaryExpression.ResultType);
    }

    public override BoundNode VisitBoundVariableDeclarationStatement(BoundVariableDeclarationStatement boundVariableDeclarationStatement)
    {
        var visitedExpression = VisitBoundExpression(boundVariableDeclarationStatement.InitializerExpression);

        if (visitedExpression is BoundConstant constant)
        {
            constantMap.Add(boundVariableDeclarationStatement.Variable, constant);
        }

        if (visitedExpression == boundVariableDeclarationStatement.InitializerExpression)
        {
            return boundVariableDeclarationStatement;
        }

        return BoundNodeFactory.CreateBoundVariableDeclarationStatement(
            syntaxNode: boundVariableDeclarationStatement.SyntaxNode,
            variable: boundVariableDeclarationStatement.Variable,
            initializerExpression: visitedExpression);
    }

    public override BoundNode VisitBoundReturnStatement(BoundReturnStatement boundReturnStatement)
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
            returnType: boundReturnStatement.ReturnType);
    }

    public override BoundNode VisitBoundClrFieldAccessExpression(BoundClrFieldAccessExpression boundClrFieldAccessExpression)
    {
        if (boundClrFieldAccessExpression.Constant)
        {
            var constantValue = CreateConstantValueFromRawValue(boundClrFieldAccessExpression.FieldInfo.GetRawConstantValue());
            if (constantValue is not null)
            {
                return BoundNodeFactory.CreateBoundConstant(
                    syntaxNode: boundClrFieldAccessExpression.SyntaxNode,
                    value: constantValue,
                    resultType: boundClrFieldAccessExpression.ResultType);
            }
        }

        return base.VisitBoundClrFieldAccessExpression(boundClrFieldAccessExpression);
    }

    // CLR literal/const fields (FieldInfo.IsLiteral) have no runtime storage slot per ECMA-335, so
    // ldsfld/ldfld against them is invalid IL. GetRawConstantValue() can return any of these raw CLR
    // types; the value is widened into ConstantInt32Value for correct IL emission (there is no
    // sub-int32 constant-load opcode), while the caller still passes the field's real (possibly
    // narrower or enum) type as the BoundConstant's resultType to preserve type fidelity.
    private ConstantValue CreateConstantValueFromRawValue(object rawValue)
    {
        return rawValue switch
        {
            null => constantValueFactory.Null,
            bool boolValue => constantValueFactory.Create(boolValue),
            string stringValue => constantValueFactory.Create(stringValue),
            float floatValue => constantValueFactory.Create(floatValue),
            double doubleValue => constantValueFactory.Create(doubleValue),
            uint uint32Value => constantValueFactory.Create(uint32Value),
            long int64Value => constantValueFactory.Create(int64Value),
            ulong uint64Value => constantValueFactory.Create(uint64Value),
            byte or sbyte or short or ushort or char or int
                => constantValueFactory.Create(System.Convert.ToInt32(rawValue)),
            _ => null
        };
    }

    public override BoundNode VisitBoundVariableExpression(BoundVariableExpression boundVariableExpression)
    {
        if (boundVariableExpression.Constant)
        {
            return constantMap[boundVariableExpression.Variable];
        }

        return boundVariableExpression;
    }
}
