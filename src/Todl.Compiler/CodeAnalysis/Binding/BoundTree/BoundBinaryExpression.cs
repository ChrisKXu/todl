using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

using BinaryOperatorIndex = ValueTuple<TypeSymbol, TypeSymbol, SyntaxKind>;

[BoundNode]
internal sealed class BoundBinaryExpression : BoundExpression
{
    public BoundBinaryOperator Operator { get; internal init; }
    public BoundExpression Left { get; internal init; }
    public BoundExpression Right { get; internal init; }

    public override TypeSymbol ResultType => Operator.ResultType;
    public override bool Constant => Left.Constant && Right.Constant;

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundBinaryExpression(this);
}

public sealed record BoundBinaryOperator(
    SyntaxKind SyntaxKind,
    BoundBinaryOperatorKind BoundBinaryOperatorKind,
    TypeSymbol ResultType);

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

    public BoundBinaryOperatorFactory(ClrTypeCache clrTypeCache)
    {
        var builtInTypes = clrTypeCache.BuiltInTypes;

        supportedBinaryOperators = new()
            {
                { (builtInTypes.Int32, builtInTypes.Int32, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.NumericAddition, builtInTypes.Int32) },
                { (builtInTypes.Int32, builtInTypes.Int32, SyntaxKind.MinusToken), new(SyntaxKind.MinusToken, BoundBinaryOperatorKind.NumericSubstraction, builtInTypes.Int32) },
                { (builtInTypes.Int32, builtInTypes.Int32, SyntaxKind.StarToken), new(SyntaxKind.StarToken, BoundBinaryOperatorKind.NumericMultiplication, builtInTypes.Int32) },
                { (builtInTypes.Int32, builtInTypes.Int32, SyntaxKind.SlashToken), new(SyntaxKind.SlashToken, BoundBinaryOperatorKind.NumericDivision, builtInTypes.Int32) },

                { (builtInTypes.Boolean, builtInTypes.Boolean, SyntaxKind.AmpersandAmpersandToken), new(SyntaxKind.AmpersandAmpersandToken, BoundBinaryOperatorKind.LogicalAnd, builtInTypes.Boolean) },
                { (builtInTypes.Boolean, builtInTypes.Boolean, SyntaxKind.PipePipeToken), new(SyntaxKind.PipePipeToken, BoundBinaryOperatorKind.LogicalOr, builtInTypes.Boolean) },

                { (builtInTypes.String, builtInTypes.String, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },

                // string concatenation with any built-in value type; the non-string side is
                // converted via ToString() at bind time (see ConvertToStringOperand below).
                { (builtInTypes.Boolean, builtInTypes.String, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },
                { (builtInTypes.String, builtInTypes.Boolean, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },
                { (builtInTypes.Byte, builtInTypes.String, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },
                { (builtInTypes.String, builtInTypes.Byte, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },
                { (builtInTypes.Char, builtInTypes.String, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },
                { (builtInTypes.String, builtInTypes.Char, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },
                { (builtInTypes.Int32, builtInTypes.String, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },
                { (builtInTypes.String, builtInTypes.Int32, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },
                { (builtInTypes.UInt32, builtInTypes.String, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },
                { (builtInTypes.String, builtInTypes.UInt32, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },
                { (builtInTypes.Int64, builtInTypes.String, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },
                { (builtInTypes.String, builtInTypes.Int64, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },
                { (builtInTypes.UInt64, builtInTypes.String, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },
                { (builtInTypes.String, builtInTypes.UInt64, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },
                { (builtInTypes.Float, builtInTypes.String, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },
                { (builtInTypes.String, builtInTypes.Float, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },
                { (builtInTypes.Double, builtInTypes.String, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },
                { (builtInTypes.String, builtInTypes.Double, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String) },

                { (builtInTypes.Int32, builtInTypes.Int32, SyntaxKind.EqualsEqualsToken), new(SyntaxKind.EqualsEqualsToken, BoundBinaryOperatorKind.Equality, builtInTypes.Boolean) },
                { (builtInTypes.Int32, builtInTypes.Int32, SyntaxKind.BangEqualsToken), new(SyntaxKind.BangEqualsToken, BoundBinaryOperatorKind.Inequality, builtInTypes.Boolean) },
                { (builtInTypes.Int32, builtInTypes.Int32, SyntaxKind.LessThanOrEqualsToken), new(SyntaxKind.LessThanOrEqualsToken, BoundBinaryOperatorKind.Comparison, builtInTypes.Boolean) },
                { (builtInTypes.Int32, builtInTypes.Int32, SyntaxKind.LessThanToken), new(SyntaxKind.LessThanToken, BoundBinaryOperatorKind.Comparison, builtInTypes.Boolean) },
                { (builtInTypes.Int32, builtInTypes.Int32, SyntaxKind.GreaterThanOrEqualsToken), new(SyntaxKind.GreaterThanOrEqualsToken, BoundBinaryOperatorKind.Comparison, builtInTypes.Boolean) },
                { (builtInTypes.Int32, builtInTypes.Int32, SyntaxKind.GreaterThanToken), new(SyntaxKind.GreaterThanToken, BoundBinaryOperatorKind.Comparison, builtInTypes.Boolean) },
            };
    }

    public BoundBinaryOperator MatchBinaryOperator(
        TypeSymbol leftResultType,
        TypeSymbol rightResultType,
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
        var boundBinaryOperator = BoundBinaryOperatorFactory.MatchBinaryOperator(boundLeft.ResultType, boundRight.ResultType, binaryExpression.Operator.Kind);

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
            boundLeft = ConvertToStringOperand(boundLeft);
            boundRight = ConvertToStringOperand(boundRight);
        }

        return BoundNodeFactory.CreateBoundBinaryExpression(
            syntaxNode: binaryExpression,
            left: boundLeft,
            right: boundRight,
            @operator: boundBinaryOperator);
    }

    // Reference-typed operands: no null-conditional support to guard a null ToString() receiver.
    private static BoundExpression ConvertToStringOperand(BoundExpression operand)
    {
        if (operand.ResultType.SpecialType == SpecialType.ClrString)
        {
            return operand;
        }

        var clrType = (operand.ResultType as ClrTypeSymbol).ClrType;
        var toStringMethod = clrType.GetMethod(nameof(ToString), Type.EmptyTypes);

        return BoundNodeFactory.CreateBoundClrFunctionCallExpression(
            syntaxNode: operand.SyntaxNode,
            boundBaseExpression: operand,
            methodInfo: toStringMethod,
            boundArguments: ImmutableArray<BoundExpression>.Empty);
    }
}
