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

    public override TypeSymbol ResultType
        => Operand.SyntaxNode.SyntaxTree.ClrTypeCache.ResolveSpecialType(Operator.ResultType);

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

    public static BoundUnaryOperatorKind GetOperandKind(this BoundUnaryOperatorKind boundUnaryOperatorKind)
        => boundUnaryOperatorKind & BoundUnaryOperatorKind.TypeMask;

    public static bool HasSideEffect(this BoundUnaryOperatorKind boundUnaryOperatorKind)
        => boundUnaryOperatorKind.GetOperationKind() switch
        {
            BoundUnaryOperatorKind.PrefixIncrement => true,
            BoundUnaryOperatorKind.PrefixDecrement => true,
            BoundUnaryOperatorKind.PostfixIncrement => true,
            BoundUnaryOperatorKind.PostfixDecrement => true,
            _ => false
        };
}

public sealed record BoundUnaryOperator(
    SyntaxKind SyntaxKind,
    BoundUnaryOperatorKind BoundUnaryOperatorKind,
    SpecialType ResultType)
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

        if (!validUnaryOperators.ContainsKey(boundUnaryOperatorKind))
        {
            return null;
        }

        return new(syntaxKind, boundUnaryOperatorKind, validUnaryOperators[boundUnaryOperatorKind]);
    }

    // reusing logic from roslyn https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Binder/Semantics/Operators/UnaryOperatorEasyOut.cs
    private static readonly ImmutableDictionary<BoundUnaryOperatorKind, SpecialType> validUnaryOperators = new Dictionary<BoundUnaryOperatorKind, SpecialType>()
    {
        // UnaryPlus
        { BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.SByte, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.Byte, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.Short, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.UShort, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.Int, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.UInt, SpecialType.ClrUInt32 },
        { BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.Long, SpecialType.ClrInt64 },
        { BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.ULong, SpecialType.ClrUInt64 },
        { BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.Float, SpecialType.ClrFloat },
        { BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.Double, SpecialType.ClrDouble },

        // UnaryMinus
        { BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.SByte, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Byte, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Short, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.UShort, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Int, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.UInt, SpecialType.ClrInt64 },
        { BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Long, SpecialType.ClrInt64 },
        { BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Float, SpecialType.ClrFloat },
        { BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Double, SpecialType.ClrDouble },

        // LogicalNegation
        { BoundUnaryOperatorKind.LogicalNegation | BoundUnaryOperatorKind.Bool, SpecialType.ClrBoolean },

        // BitwiseComplement
        { BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.SByte, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.Byte, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.Short, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.UShort, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.Int, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.UInt, SpecialType.ClrUInt32 },
        { BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.Long, SpecialType.ClrInt64 },
        { BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.ULong, SpecialType.ClrUInt64 },

        // PostfixIncrement
        { BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.SByte, SpecialType.ClrSByte },
        { BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.Byte, SpecialType.ClrByte },
        { BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.Short, SpecialType.ClrInt16 },
        { BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.UShort, SpecialType.ClrUInt16 },
        { BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.Int, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.UInt, SpecialType.ClrUInt32 },
        { BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.Long, SpecialType.ClrInt64 },
        { BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.ULong, SpecialType.ClrUInt64 },
        { BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.Float, SpecialType.ClrFloat },
        { BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.Double, SpecialType.ClrDouble },

        // PostfixDecrement
        { BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.SByte, SpecialType.ClrSByte },
        { BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.Byte, SpecialType.ClrByte },
        { BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.Short, SpecialType.ClrInt16 },
        { BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.UShort, SpecialType.ClrUInt16 },
        { BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.Int, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.UInt, SpecialType.ClrUInt32 },
        { BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.Long, SpecialType.ClrInt64 },
        { BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.ULong, SpecialType.ClrUInt64 },
        { BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.Float, SpecialType.ClrFloat },
        { BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.Double, SpecialType.ClrDouble },

        // PrefixIncrement
        { BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.SByte, SpecialType.ClrSByte },
        { BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.Byte, SpecialType.ClrByte },
        { BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.Short, SpecialType.ClrInt16 },
        { BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.UShort, SpecialType.ClrUInt16 },
        { BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.Int, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.UInt, SpecialType.ClrUInt32 },
        { BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.Long, SpecialType.ClrInt64 },
        { BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.ULong, SpecialType.ClrUInt64 },
        { BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.Float, SpecialType.ClrFloat },
        { BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.Double, SpecialType.ClrDouble },

        // PrefixDecrement
        { BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.SByte, SpecialType.ClrSByte },
        { BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.Byte, SpecialType.ClrByte },
        { BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.Short, SpecialType.ClrInt16 },
        { BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.UShort, SpecialType.ClrUInt16 },
        { BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.Int, SpecialType.ClrInt32 },
        { BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.UInt, SpecialType.ClrUInt32 },
        { BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.Long, SpecialType.ClrInt64 },
        { BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.ULong, SpecialType.ClrUInt64 },
        { BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.Float, SpecialType.ClrFloat },
        { BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.Double, SpecialType.ClrDouble },
    }.ToImmutableDictionary();
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

        if (boundUnaryOperator is null)
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

        if (!boundOperand.LValue && boundUnaryOperator.BoundUnaryOperatorKind.HasSideEffect())
        {
            diagnosticBuilder.Add(
                new Diagnostic()
                {
                    Message = $"Unary operator \"{unaryExpression.Operator.Text}\" requires an lvalue.",
                    Level = DiagnosticLevel.Error,
                    TextLocation = unaryExpression.Operator.GetTextLocation(),
                    ErrorCode = ErrorCode.NotAnLValue
                });
        }
        else if (boundOperand.ReadOnly && boundUnaryOperator.BoundUnaryOperatorKind.HasSideEffect())
        {
            diagnosticBuilder.Add(
                new Diagnostic()
                {
                    Message = $"Expression \"{unaryExpression.Operand.Text}\" is read only.",
                    Level = DiagnosticLevel.Error,
                    TextLocation = unaryExpression.Operand.Text.GetTextLocation(),
                    ErrorCode = ErrorCode.ReadOnlyVariable
                });
        }

        return BoundNodeFactory.CreateBoundUnaryExpression(
            syntaxNode: unaryExpression,
            operand: boundOperand,
            @operator: boundUnaryOperator,
            diagnosticBuilder: diagnosticBuilder);
    }
}
