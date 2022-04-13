using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    using UnaryOperatorIndex = Tuple<TypeSymbol, SyntaxKind, bool>;

    public sealed class BoundUnaryExpression : BoundExpression
    {
        public sealed class BoundUnaryOperator
        {
            public SyntaxKind SyntaxKind { get; }
            public BoundUnaryOperatorKind BoundUnaryOperatorKind { get; }
            public TypeSymbol ResultType { get; }

            public BoundUnaryOperator(SyntaxKind syntaxKind, BoundUnaryOperatorKind boundUnaryOperatorKind, TypeSymbol resultType)
            {
                SyntaxKind = syntaxKind;
                BoundUnaryOperatorKind = boundUnaryOperatorKind;
                ResultType = resultType;
            }
        }

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

        internal static BoundUnaryOperator MatchUnaryOperator(TypeSymbol operandResultType, SyntaxKind syntaxKind, bool trailing)
            => supportedUnaryOperators.GetValueOrDefault(Tuple.Create(operandResultType, syntaxKind, trailing), null);

        public BoundUnaryOperator Operator { get; internal init; }
        public BoundExpression Operand { get; internal init; }

        public override TypeSymbol ResultType => Operator.ResultType;
        public override bool Constant => Operand.Constant;

        private BuiltInTypes BuiltInTypes => SyntaxNode.SyntaxTree.ClrTypeCache.BuiltInTypes;
    }

    public partial class Binder
    {
        private BoundExpression BindUnaryExpression(UnaryExpression unaryExpression)
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var boundOperand = BindExpression(unaryExpression.Operand);
            var boundUnaryOperator = BoundUnaryExpression.MatchUnaryOperator(
                operandResultType: boundOperand.ResultType,
                syntaxKind: unaryExpression.Operator.Kind,
                trailing: unaryExpression.Trailing);

            if (boundUnaryOperator == null)
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
