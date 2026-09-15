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

                { (builtInTypes.Int32, builtInTypes.Int32, SyntaxKind.EqualsEqualsToken), new(SyntaxKind.EqualsEqualsToken, BoundBinaryOperatorKind.Equality, builtInTypes.Boolean) },
                { (builtInTypes.Int32, builtInTypes.Int32, SyntaxKind.BangEqualsToken), new(SyntaxKind.BangEqualsToken, BoundBinaryOperatorKind.Inequality, builtInTypes.Boolean) },
                { (builtInTypes.Int32, builtInTypes.Int32, SyntaxKind.LessThanOrEqualsToken), new(SyntaxKind.LessThanOrEqualsToken, BoundBinaryOperatorKind.Comparison, builtInTypes.Boolean) },
                { (builtInTypes.Int32, builtInTypes.Int32, SyntaxKind.LessThanToken), new(SyntaxKind.LessThanToken, BoundBinaryOperatorKind.Comparison, builtInTypes.Boolean) },
                { (builtInTypes.Int32, builtInTypes.Int32, SyntaxKind.GreaterThanOrEqualsToken), new(SyntaxKind.GreaterThanOrEqualsToken, BoundBinaryOperatorKind.Comparison, builtInTypes.Boolean) },
                { (builtInTypes.Int32, builtInTypes.Int32, SyntaxKind.GreaterThanToken), new(SyntaxKind.GreaterThanToken, BoundBinaryOperatorKind.Comparison, builtInTypes.Boolean) },
            };

        // string + <built-in value type> and <built-in value type> + string: the non-string
        // side gets converted via ToString() when binding (see BindBinaryExpression below).
        foreach (var valueType in new[]
        {
            builtInTypes.Boolean, builtInTypes.Byte, builtInTypes.Char, builtInTypes.Int32,
            builtInTypes.UInt32, builtInTypes.Int64, builtInTypes.UInt64, builtInTypes.Float, builtInTypes.Double
        })
        {
            supportedBinaryOperators.Add((valueType, builtInTypes.String, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String));
            supportedBinaryOperators.Add((builtInTypes.String, valueType, SyntaxKind.PlusToken), new(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, builtInTypes.String));
        }
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

    // Value-type operands in a string concatenation are converted via their own ToString()
    // override; reference types (other than string itself) aren't supported here since a null
    // receiver would crash - see BoundBinaryOperatorFactory for which types this applies to.
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
            boundArguments: []);
    }
}
