using System;
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
        BoundUnaryOperatorKind boundUnaryOperatorKind = default;

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



        return new(syntaxKind, boundUnaryOperatorKind, operandType);
    }

    public bool Validate()
    {
        if (BoundUnaryOperatorKind == BoundUnaryOperatorKind.Error)
        {
            return false;
        }

        return true;
    }
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
                    Message = $"Operator {unaryExpression.Operator.Text} is not supported on type {boundOperand.ResultType.Name}",
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
