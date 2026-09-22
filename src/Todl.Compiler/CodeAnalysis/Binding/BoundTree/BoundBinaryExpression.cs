using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

using BinaryOperatorIndex = ValueTuple<SpecialType, SpecialType, SyntaxKind>;

[BoundNode]
internal sealed class BoundBinaryExpression : BoundExpression
{
    public BoundBinaryOperator Operator { get; internal init; }
    public BoundExpression Left { get; internal init; }
    public BoundExpression Right { get; internal init; }
    public ClrTypeCache ClrTypeCache { get; internal init; }

    public override TypeSymbol ResultType => ClrTypeCache.ResolveSpecialType(Operator.ResultType);
    public override bool Constant => Left.Constant && Right.Constant;

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundBinaryExpression(this);
}

public sealed record BoundBinaryOperator(
    SyntaxKind SyntaxKind,
    BoundBinaryOperatorKind BoundBinaryOperatorKind,
    SpecialType ResultType);

public enum BoundBinaryOperatorKind
{
    // Numeric
    NumericAddition,
    NumericSubstraction,
    NumericMultiplication,
    NumericDivision,

    // Logical
    LogicalAnd,
    LogicalOr,

    // Comparison
    Equality,
    Inequality,
    Comparison,

    // String
    StringConcatenation
}

public sealed class BoundBinaryOperatorFactory
{
    private readonly Dictionary<BinaryOperatorIndex, BoundBinaryOperator> supportedBinaryOperators;

    public BoundBinaryOperatorFactory()
    {
        supportedBinaryOperators = new()
            {
                { (SpecialType.ClrInt32, SpecialType.ClrInt32, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.NumericAddition, SpecialType.ClrInt32) },
                { (SpecialType.ClrInt32, SpecialType.ClrInt32, SyntaxKind.MinusToken), new(SyntaxKind.MinusToken, BoundBinaryOperatorKind.NumericSubstraction, SpecialType.ClrInt32) },
                { (SpecialType.ClrInt32, SpecialType.ClrInt32, SyntaxKind.StarToken), new(SyntaxKind.StarToken, BoundBinaryOperatorKind.NumericMultiplication, SpecialType.ClrInt32) },
                { (SpecialType.ClrInt32, SpecialType.ClrInt32, SyntaxKind.SlashToken), new(SyntaxKind.SlashToken, BoundBinaryOperatorKind.NumericDivision, SpecialType.ClrInt32) },

                { (SpecialType.ClrBoolean, SpecialType.ClrBoolean, SyntaxKind.AmpersandAmpersandToken), new(SyntaxKind.AmpersandAmpersandToken, BoundBinaryOperatorKind.LogicalAnd, SpecialType.ClrBoolean) },
                { (SpecialType.ClrBoolean, SpecialType.ClrBoolean, SyntaxKind.PipePipeToken), new(SyntaxKind.PipePipeToken, BoundBinaryOperatorKind.LogicalOr, SpecialType.ClrBoolean) },

                { (SpecialType.ClrString, SpecialType.ClrString, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },

                // string concatenation with any built-in value type; the non-string side is
                // converted via ToString() at bind time (see ConvertToStringOperand below).
                { (SpecialType.ClrBoolean, SpecialType.ClrString, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },
                { (SpecialType.ClrString, SpecialType.ClrBoolean, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },
                { (SpecialType.ClrByte, SpecialType.ClrString, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },
                { (SpecialType.ClrString, SpecialType.ClrByte, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },
                { (SpecialType.ClrChar, SpecialType.ClrString, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },
                { (SpecialType.ClrString, SpecialType.ClrChar, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },
                { (SpecialType.ClrInt32, SpecialType.ClrString, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },
                { (SpecialType.ClrString, SpecialType.ClrInt32, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },
                { (SpecialType.ClrUInt32, SpecialType.ClrString, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },
                { (SpecialType.ClrString, SpecialType.ClrUInt32, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },
                { (SpecialType.ClrInt64, SpecialType.ClrString, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },
                { (SpecialType.ClrString, SpecialType.ClrInt64, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },
                { (SpecialType.ClrUInt64, SpecialType.ClrString, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },
                { (SpecialType.ClrString, SpecialType.ClrUInt64, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },
                { (SpecialType.ClrFloat, SpecialType.ClrString, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },
                { (SpecialType.ClrString, SpecialType.ClrFloat, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },
                { (SpecialType.ClrDouble, SpecialType.ClrString, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },
                { (SpecialType.ClrString, SpecialType.ClrDouble, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, SpecialType.ClrString) },

                { (SpecialType.ClrInt32, SpecialType.ClrInt32, SyntaxKind.EqualsEqualsToken), new(SyntaxKind.EqualsEqualsToken, BoundBinaryOperatorKind.Equality, SpecialType.ClrBoolean) },
                { (SpecialType.ClrInt32, SpecialType.ClrInt32, SyntaxKind.BangEqualsToken), new(SyntaxKind.BangEqualsToken, BoundBinaryOperatorKind.Inequality, SpecialType.ClrBoolean) },
                { (SpecialType.ClrInt32, SpecialType.ClrInt32, SyntaxKind.LessThanOrEqualsToken), new(SyntaxKind.LessThanOrEqualsToken, BoundBinaryOperatorKind.Comparison, SpecialType.ClrBoolean) },
                { (SpecialType.ClrInt32, SpecialType.ClrInt32, SyntaxKind.LessThanToken), new(SyntaxKind.LessThanToken, BoundBinaryOperatorKind.Comparison, SpecialType.ClrBoolean) },
                { (SpecialType.ClrInt32, SpecialType.ClrInt32, SyntaxKind.GreaterThanOrEqualsToken), new(SyntaxKind.GreaterThanOrEqualsToken, BoundBinaryOperatorKind.Comparison, SpecialType.ClrBoolean) },
                { (SpecialType.ClrInt32, SpecialType.ClrInt32, SyntaxKind.GreaterThanToken), new(SyntaxKind.GreaterThanToken, BoundBinaryOperatorKind.Comparison, SpecialType.ClrBoolean) },
            };
    }

    public BoundBinaryOperator MatchBinaryOperator(
        SpecialType leftResultType,
        SpecialType rightResultType,
        SyntaxKind syntaxKind)
    {
        return supportedBinaryOperators.GetValueOrDefault((leftResultType, rightResultType, syntaxKind));
    }
}

public partial class Binder
{
    private BoundBinaryExpression BindBinaryExpression(BinaryExpression binaryExpression)
    {
        var boundLeft = BindExpression(binaryExpression.Left);
        var boundRight = BindExpression(binaryExpression.Right);
        var boundBinaryOperator = BoundBinaryOperatorFactory.MatchBinaryOperator(boundLeft.ResultType.SpecialType, boundRight.ResultType.SpecialType, binaryExpression.Operator.Kind);

        if (boundBinaryOperator is null)
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = $"Operator {binaryExpression.Operator.Text} is not supported on types {boundLeft.ResultType.Name} and {boundRight.ResultType.Name}",
                    Level = DiagnosticLevel.Error,
                    TextLocation = binaryExpression.GetTextLocation(binaryExpression.Operator.Span),
                    ErrorCode = ErrorCode.UnsupportedOperator
                });
        }
        else if (boundBinaryOperator.BoundBinaryOperatorKind == BoundBinaryOperatorKind.StringConcatenation)
        {
            boundLeft = ConvertToStringOperand(boundLeft, ClrTypeCache);
            boundRight = ConvertToStringOperand(boundRight, ClrTypeCache);
        }

        return BoundNodeFactory.CreateBoundBinaryExpression(
            syntaxNode: binaryExpression,
            left: boundLeft,
            right: boundRight,
            @operator: boundBinaryOperator,
            clrTypeCache: ClrTypeCache);
    }

    // Reference-typed operands: no null-conditional support to guard a null ToString() receiver.
    private static BoundExpression ConvertToStringOperand(BoundExpression operand, ClrTypeCache clrTypeCache)
    {
        if (operand.ResultType.SpecialType == SpecialType.ClrString)
        {
            return operand;
        }

        var clrType = (operand.ResultType as ClrTypeSymbol).ClrType;
        var toStringMethod = clrType.GetMethod(nameof(ToString), Type.EmptyTypes);

        return BoundNodeFactory.CreateBoundClrInvocationExpression(
            syntaxNode: operand.SyntaxNode,
            boundBaseExpression: operand,
            methodInfo: toStringMethod,
            boundArguments: ImmutableArray<BoundExpression>.Empty,
            clrTypeCache: clrTypeCache);
    }
}
