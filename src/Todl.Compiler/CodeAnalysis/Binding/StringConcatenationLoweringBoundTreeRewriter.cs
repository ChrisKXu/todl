using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding;

// Runs after ConstantFoldingBoundTreeRewriter; also merges constants that only become adjacent once a chain is flattened.
internal sealed class StringConcatenationLoweringBoundTreeRewriter : BoundTreeRewriter
{
    private static readonly MethodInfo concat2Method = ResolveConcatMethod(2);
    private static readonly MethodInfo concat3Method = ResolveConcatMethod(3);
    private static readonly MethodInfo concat4Method = ResolveConcatMethod(4);

    private readonly ConstantValueFactory constantValueFactory;

    public StringConcatenationLoweringBoundTreeRewriter(ConstantValueFactory constantValueFactory)
    {
        this.constantValueFactory = constantValueFactory;
    }

    public override BoundNode VisitBoundBinaryExpression(BoundBinaryExpression boundBinaryExpression)
    {
        if (boundBinaryExpression.Operator.BoundBinaryOperatorKind != BoundBinaryOperatorKind.StringConcatenation)
        {
            return base.VisitBoundBinaryExpression(boundBinaryExpression);
        }

        var operands = new List<BoundExpression>();
        AppendOperand(boundBinaryExpression.Left, operands);
        AppendOperand(boundBinaryExpression.Right, operands);

        return CreateConcatCall(boundBinaryExpression.SyntaxNode, operands);
    }

    // Flattens a StringConcatenation chain into leaf operands, left to right.
    private void AppendOperand(BoundExpression expression, List<BoundExpression> operands)
    {
        if (expression is BoundBinaryExpression { Operator.BoundBinaryOperatorKind: BoundBinaryOperatorKind.StringConcatenation } nested)
        {
            AppendOperand(nested.Left, operands);
            AppendOperand(nested.Right, operands);
            return;
        }

        AppendMerged(VisitBoundExpression(expression), operands);
    }

    // Merges adjacent constant string operands into one.
    private void AppendMerged(BoundExpression operand, List<BoundExpression> operands)
    {
        if (operand is BoundConstant { Value: ConstantStringValue currentValue }
            && operands.Count > 0
            && operands[^1] is BoundConstant { Value: ConstantStringValue previousValue } previousConstant)
        {
            operands[^1] = BoundNodeFactory.CreateBoundConstant(
                syntaxNode: previousConstant.SyntaxNode,
                value: constantValueFactory.Create(previousValue.Value + currentValue.Value));
            return;
        }

        operands.Add(operand);
    }

    private static BoundExpression CreateConcatCall(SyntaxNode syntaxNode, IReadOnlyList<BoundExpression> operands)
    {
        if (operands.Count <= 4)
        {
            return CreateCall(syntaxNode, GetConcatMethod(operands.Count), operands);
        }

        // No array-creation bound node exists for Concat(string[]); fold left-to-right instead.
        var result = CreateCall(syntaxNode, concat4Method, operands.Take(4).ToArray());

        for (var i = 4; i < operands.Count; i++)
        {
            result = CreateCall(syntaxNode, concat2Method, new[] { result, operands[i] });
        }

        return result;
    }

    private static BoundExpression CreateCall(SyntaxNode syntaxNode, MethodInfo methodInfo, IReadOnlyList<BoundExpression> arguments)
        => BoundNodeFactory.CreateBoundClrFunctionCallExpression(
            syntaxNode: syntaxNode,
            boundBaseExpression: arguments[0],
            methodInfo: methodInfo,
            boundArguments: arguments.ToImmutableArray());

    private static MethodInfo GetConcatMethod(int operandCount)
        => operandCount switch
        {
            2 => concat2Method,
            3 => concat3Method,
            4 => concat4Method,
            _ => throw new ArgumentOutOfRangeException(nameof(operandCount), operandCount, "Expected between 2 and 4 operands")
        };

    private static MethodInfo ResolveConcatMethod(int arity)
        => typeof(string).GetMethod(nameof(string.Concat), Enumerable.Repeat(typeof(string), arity).ToArray());
}
