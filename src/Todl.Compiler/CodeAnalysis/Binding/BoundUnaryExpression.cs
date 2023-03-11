using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundUnaryExpression : BoundExpression
{
    public BoundUnaryOperator Operator { get; internal init; }
    public BoundExpression Operand { get; internal init; }

    public override TypeSymbol ResultType => Operator.ResultType;
    public override bool Constant => Operand.Constant;
}

// Values are copied from https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Binder/Semantics/Operators/OperatorKind.cs
// Some of the values are not used but reserved for forward compatibility
[Flags]
public enum BoundUnaryOperatorKind
{
    TypeMask = 0x00000FF,

    SByte = 0x00000001,
    Byte = 0x00000002,
    Short = 0x00000003,
    UShort = 0x00000004,
    Int = 0x00000005,
    UInt = 0x00000006,
    Long = 0x00000007,
    ULong = 0x00000008,
    NInt = 0x00000009,
    NUInt = 0x0000000A,
    Char = 0x0000000B,
    Float = 0x0000000C,
    Double = 0x0000000D,
    Decimal = 0x0000000E,
    Bool = 0x0000000F,

    OpMask = 0x0000FF00,

    PostfixIncrement = 0x00001000,
    PostfixDecrement = 0x00001100,
    PrefixIncrement = 0x00001200,
    PrefixDecrement = 0x00001300,
    UnaryPlus = 0x00001400,
    UnaryMinus = 0x00001500,
    LogicalNegation = 0x00001600,
    BitwiseComplement = 0x00001700,

    Error = 0x00000000
}

public static class BoundUnaryOperatorKindExtensions
{
    public static BoundUnaryOperatorKind GetOperationKind(this BoundUnaryOperatorKind boundUnaryOperatorKind)
        => boundUnaryOperatorKind & BoundUnaryOperatorKind.OpMask;
}

public sealed record BoundUnaryOperator(
    SyntaxKind SyntaxKind,
    BoundUnaryOperatorKind BoundUnaryOperatorKind,
    TypeSymbol ResultType)
{
    public static BoundUnaryOperator Create(
        TypeSymbol operandType,
        SyntaxKind syntaxKind,
        bool trailing)
    {
        var boundUnaryOperatorKind = BoundUnaryOperatorKind.Error;

        boundUnaryOperatorKind |= syntaxKind switch
        {
            SyntaxKind.PlusToken => BoundUnaryOperatorKind.UnaryPlus,
            SyntaxKind.MinusToken => BoundUnaryOperatorKind.UnaryMinus,
            SyntaxKind.BangToken => BoundUnaryOperatorKind.LogicalNegation,
            SyntaxKind.TildeToken => BoundUnaryOperatorKind.BitwiseComplement,
            SyntaxKind.PlusPlusToken => trailing ? BoundUnaryOperatorKind.PostfixIncrement : BoundUnaryOperatorKind.PrefixIncrement,
            SyntaxKind.MinusMinusToken => trailing ? BoundUnaryOperatorKind.PostfixDecrement : BoundUnaryOperatorKind.PrefixDecrement,
            _ => BoundUnaryOperatorKind.Error
        };

        boundUnaryOperatorKind |= operandType.SpecialType switch
        {
            SpecialType.ClrBoolean => BoundUnaryOperatorKind.Bool,
            SpecialType.ClrByte => BoundUnaryOperatorKind.Byte,
            SpecialType.ClrInt32 => BoundUnaryOperatorKind.Int,
            SpecialType.ClrUInt32 => BoundUnaryOperatorKind.UInt,
            SpecialType.ClrInt64 => BoundUnaryOperatorKind.Long,
            SpecialType.ClrUInt64 => BoundUnaryOperatorKind.ULong,
            SpecialType.ClrFloat => BoundUnaryOperatorKind.Float,
            SpecialType.ClrDouble => BoundUnaryOperatorKind.Double,
            _ => BoundUnaryOperatorKind.Error
        };

        return new(syntaxKind, boundUnaryOperatorKind, operandType);
    }

    public bool Validate()
        => validUnaryOperators.Contains(BoundUnaryOperatorKind);

    private static readonly ImmutableHashSet<BoundUnaryOperatorKind> validUnaryOperators = new HashSet<BoundUnaryOperatorKind>()
    {
        // UnaryPlus
        BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.SByte,
        BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.Byte,
        BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.Short,
        BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.UShort,
        BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.Int,
        BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.UInt,
        BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.Long,
        BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.ULong,
        BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.Float,
        BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.Double,

        // UnaryMinus
        BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.SByte,
        BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Byte,
        BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Short,
        BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.UShort,
        BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Int,
        BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.UInt,
        BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Long,
        BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Float,
        BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Double,

        // LogicalNegation
        BoundUnaryOperatorKind.LogicalNegation | BoundUnaryOperatorKind.Bool,

        // BitwiseComplement
        BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.SByte,
        BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.Byte,
        BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.Short,
        BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.UShort,
        BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.Int,
        BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.UInt,
        BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.Long,
        BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.ULong,

        // PostfixIncrement
        BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.SByte,
        BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.Byte,
        BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.Short,
        BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.UShort,
        BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.Int,
        BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.UInt,
        BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.Long,
        BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.ULong,
        BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.Float,
        BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.Double,

        // PostfixDecrement
        BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.SByte,
        BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.Byte,
        BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.Short,
        BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.UShort,
        BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.Int,
        BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.UInt,
        BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.Long,
        BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.ULong,
        BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.Float,
        BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.Double,

        // PrefixIncrement
        BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.SByte,
        BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.Byte,
        BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.Short,
        BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.UShort,
        BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.Int,
        BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.UInt,
        BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.Long,
        BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.ULong,
        BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.Float,
        BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.Double,

        // PrefixDecrement
        BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.SByte,
        BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.Byte,
        BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.Short,
        BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.UShort,
        BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.Int,
        BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.UInt,
        BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.Long,
        BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.ULong,
        BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.Float,
        BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.Double
    }.ToImmutableHashSet();
}

public partial class Binder
{
    private BoundExpression BindUnaryExpression(UnaryExpression unaryExpression)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundOperand = BindExpression(unaryExpression.Operand);
        var boundUnaryOperator = BoundUnaryOperator.Create(
            operandType: boundOperand.ResultType,
            syntaxKind: unaryExpression.Operator.Kind,
            trailing: unaryExpression.Trailing);

        if (!boundUnaryOperator.Validate())
        {
            diagnosticBuilder.Add(
                new Diagnostic()
                {
                    Message = $"Unary operator \"{unaryExpression.Operator.Text}\" is not supported on type \"{boundOperand.ResultType.Name}\"",
                    Level = DiagnosticLevel.Error,
                    TextLocation = unaryExpression.Operator.GetTextLocation(),
                    ErrorCode = ErrorCode.UnsupportedOperator
                });
        }

        return BoundNodeFactory.CreateBoundUnaryExpression(
            syntaxNode: unaryExpression,
            operand: boundOperand,
            @operator: boundUnaryOperator,
            diagnosticBuilder: diagnosticBuilder);
    }
}
