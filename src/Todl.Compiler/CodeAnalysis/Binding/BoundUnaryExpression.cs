using System;
using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    using UnaryOperatorIndex = ValueTuple<TypeSymbol, SyntaxKind, bool>;

    public sealed class BoundUnaryExpression : BoundExpression
    {
        private static readonly IReadOnlyDictionary<UnaryOperatorIndex, BoundUnaryOperator> supportedUnaryOperators = new Dictionary<UnaryOperatorIndex, BoundUnaryOperator>
        {
            //{ Tuple.Create(TypeSymbol.ClrInt32, SyntaxKind.PlusToken, false), new BoundUnaryOperator(SyntaxKind.PlusToken, BoundUnaryOperatorKind.Identity, TypeSymbol.ClrInt32) },
            //{ Tuple.Create(TypeSymbol.ClrInt64, SyntaxKind.PlusToken, false), new BoundUnaryOperator(SyntaxKind.PlusToken, BoundUnaryOperatorKind.Identity, TypeSymbol.ClrInt64) },
            //{ Tuple.Create(TypeSymbol.ClrInt32, SyntaxKind.MinusToken, false), new BoundUnaryOperator(SyntaxKind.MinusToken, BoundUnaryOperatorKind.Negation, TypeSymbol.ClrInt32) },
            //{ Tuple.Create(TypeSymbol.ClrInt64, SyntaxKind.MinusToken, false), new BoundUnaryOperator(SyntaxKind.MinusToken, BoundUnaryOperatorKind.Negation, TypeSymbol.ClrInt64) },

            //{ Tuple.Create(TypeSymbol.ClrInt32, SyntaxKind.PlusPlusToken, false), new BoundUnaryOperator(SyntaxKind.PlusPlusToken, BoundUnaryOperatorKind.PreIncrement, TypeSymbol.ClrInt32) },
            //{ Tuple.Create(TypeSymbol.ClrInt64, SyntaxKind.PlusPlusToken, false), new BoundUnaryOperator(SyntaxKind.PlusPlusToken, BoundUnaryOperatorKind.PreIncrement, TypeSymbol.ClrInt64) },
            //{ Tuple.Create(TypeSymbol.ClrInt32, SyntaxKind.PlusPlusToken, true), new BoundUnaryOperator(SyntaxKind.PlusPlusToken, BoundUnaryOperatorKind.PostIncrement, TypeSymbol.ClrInt32) },
            //{ Tuple.Create(TypeSymbol.ClrInt64, SyntaxKind.PlusPlusToken, true), new BoundUnaryOperator(SyntaxKind.PlusPlusToken, BoundUnaryOperatorKind.PostIncrement, TypeSymbol.ClrInt64) },
            //{ Tuple.Create(TypeSymbol.ClrInt32, SyntaxKind.MinusMinusToken, false), new BoundUnaryOperator(SyntaxKind.MinusMinusToken, BoundUnaryOperatorKind.PreDecrement, TypeSymbol.ClrInt32) },
            //{ Tuple.Create(TypeSymbol.ClrInt64, SyntaxKind.MinusMinusToken, false), new BoundUnaryOperator(SyntaxKind.MinusMinusToken, BoundUnaryOperatorKind.PreDecrement, TypeSymbol.ClrInt64) },
            //{ Tuple.Create(TypeSymbol.ClrInt32, SyntaxKind.MinusMinusToken, true), new BoundUnaryOperator(SyntaxKind.MinusMinusToken, BoundUnaryOperatorKind.PostDecrement, TypeSymbol.ClrInt32) },
            //{ Tuple.Create(TypeSymbol.ClrInt64, SyntaxKind.MinusMinusToken, true), new BoundUnaryOperator(SyntaxKind.MinusMinusToken, BoundUnaryOperatorKind.PostDecrement, TypeSymbol.ClrInt64) },

            //{ Tuple.Create(TypeSymbol.ClrBoolean, SyntaxKind.BangToken, false), new BoundUnaryOperator(SyntaxKind.BangToken, BoundUnaryOperatorKind.LogicalNegation, TypeSymbol.ClrBoolean) }
        };

        public BoundUnaryOperator Operator { get; internal init; }
        public BoundExpression Operand { get; internal init; }

        public override TypeSymbol ResultType => Operator.ResultType;
        public override bool Constant => Operand.Constant;
    }

    public sealed record BoundUnaryOperator(
        SyntaxKind SyntaxKind,
        BoundUnaryOperatorKind BoundUnaryOperatorKind,
        TypeSymbol ResultType);

    public enum BoundUnaryOperatorKind
    {
        // Arithmetic
        Identity,         // +a
        Negation,         // -a
        PreIncrement,     // ++i
        PreDecrement,     // --i
        PostIncrement,    // i++
        PostDecrement,    // i--

        // Logical
        LogicalNegation   // !isNative
    }

    public sealed class BoundUnaryOperatorFactory
    {
        private readonly Dictionary<UnaryOperatorIndex, BoundUnaryOperator> supportedUnaryOperators;

        internal BoundUnaryOperatorFactory(ClrTypeCache clrTypeCache)
        {
            var builtInTypes = clrTypeCache.BuiltInTypes;

            supportedUnaryOperators = new()
            {
                { (builtInTypes.Int32, SyntaxKind.PlusToken, false), new(SyntaxKind.PlusToken, BoundUnaryOperatorKind.Identity, builtInTypes.Int32) },
                { (builtInTypes.Int32, SyntaxKind.MinusToken, false), new(SyntaxKind.MinusToken, BoundUnaryOperatorKind.Negation, builtInTypes.Int32) },
                { (builtInTypes.Int32, SyntaxKind.PlusPlusToken, false), new(SyntaxKind.PlusPlusToken, BoundUnaryOperatorKind.PreIncrement, builtInTypes.Int32) },
                { (builtInTypes.Int32, SyntaxKind.MinusMinusToken, false), new(SyntaxKind.MinusMinusToken, BoundUnaryOperatorKind.PreDecrement, builtInTypes.Int32) },
                { (builtInTypes.Int32, SyntaxKind.PlusPlusToken, true), new(SyntaxKind.PlusPlusToken, BoundUnaryOperatorKind.PostIncrement, builtInTypes.Int32) },
                { (builtInTypes.Int32, SyntaxKind.MinusMinusToken, true), new(SyntaxKind.MinusMinusToken, BoundUnaryOperatorKind.PostDecrement, builtInTypes.Int32) },
                { (builtInTypes.Boolean, SyntaxKind.BangToken, false), new(SyntaxKind.BangToken, BoundUnaryOperatorKind.LogicalNegation, builtInTypes.Boolean) }
            };
        }

        internal BoundUnaryOperator MatchUnaryOperator(TypeSymbol operandType, SyntaxKind syntaxKind, bool trailing)
            => supportedUnaryOperators.GetValueOrDefault((operandType, syntaxKind, trailing));
    }

    public partial class Binder
    {
        private BoundExpression BindUnaryExpression(UnaryExpression unaryExpression)
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var boundOperand = BindExpression(unaryExpression.Operand);
            var boundUnaryOperator = BoundUnaryOperatorFactory.MatchUnaryOperator(
                operandType: boundOperand.ResultType,
                syntaxKind: unaryExpression.Operator.Kind,
                trailing: unaryExpression.Trailing);

            if (boundUnaryOperator is null)
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
}
