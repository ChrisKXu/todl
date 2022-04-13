using System;
using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    using BinaryOperatorIndex = Tuple<TypeSymbol, TypeSymbol, SyntaxKind>;

    public sealed class BoundBinaryExpression : BoundExpression
    {
        public sealed class BoundBinaryOperator
        {
            public SyntaxKind SyntaxKind { get; }
            public BoundBinaryOperatorKind BoundBinaryOperatorKind { get; }
            public TypeSymbol ResultType { get; }

            public BoundBinaryOperator(SyntaxKind syntaxKind, BoundBinaryOperatorKind boundBinaryOperatorKind, TypeSymbol resultType)
            {
                SyntaxKind = syntaxKind;
                BoundBinaryOperatorKind = boundBinaryOperatorKind;
                ResultType = resultType;
            }
        }

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

            // String
            StringConcatenation
        }

        private static readonly IReadOnlyDictionary<BinaryOperatorIndex, BoundBinaryOperator> supportedBinaryOperators = new Dictionary<BinaryOperatorIndex, BoundBinaryOperator>
        {
            //{ Tuple.Create(TypeSymbol.ClrInt32, TypeSymbol.ClrInt32, SyntaxKind.PlusToken), new BoundBinaryOperator(SyntaxKind.PlusToken, BoundBinaryOperatorKind.NumericAddition, TypeSymbol.ClrInt32) },
            //{ Tuple.Create(TypeSymbol.ClrInt32, TypeSymbol.ClrInt32, SyntaxKind.MinusToken), new BoundBinaryOperator(SyntaxKind.MinusToken, BoundBinaryOperatorKind.NumericSubstraction, TypeSymbol.ClrInt32) },
            //{ Tuple.Create(TypeSymbol.ClrInt32, TypeSymbol.ClrInt32, SyntaxKind.StarToken), new BoundBinaryOperator(SyntaxKind.StarToken, BoundBinaryOperatorKind.NumericMultiplication, TypeSymbol.ClrInt32) },
            //{ Tuple.Create(TypeSymbol.ClrInt32, TypeSymbol.ClrInt32, SyntaxKind.SlashToken), new BoundBinaryOperator(SyntaxKind.SlashToken, BoundBinaryOperatorKind.NumericDivision, TypeSymbol.ClrInt32) },

            //{ Tuple.Create(TypeSymbol.ClrBoolean, TypeSymbol.ClrBoolean, SyntaxKind.AmpersandAmpersandToken), new BoundBinaryOperator(SyntaxKind.AmpersandAmpersandToken, BoundBinaryOperatorKind.LogicalAnd, TypeSymbol.ClrBoolean) },
            //{ Tuple.Create(TypeSymbol.ClrBoolean, TypeSymbol.ClrBoolean, SyntaxKind.PipePipeToken), new BoundBinaryOperator(SyntaxKind.PipePipeToken, BoundBinaryOperatorKind.LogicalOr, TypeSymbol.ClrBoolean) },

            //{ Tuple.Create(TypeSymbol.ClrString, TypeSymbol.ClrString, SyntaxKind.PlusToken), new BoundBinaryOperator(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, TypeSymbol.ClrString) }
        };

        internal static BoundBinaryOperator MatchBinaryOperator(TypeSymbol leftResultType, TypeSymbol rightResultType, SyntaxKind syntaxKind)
            => supportedBinaryOperators.GetValueOrDefault(Tuple.Create(leftResultType, rightResultType, syntaxKind), null);

        public BoundBinaryOperator Operator { get; internal init; }
        public BoundExpression Left { get; internal init; }
        public BoundExpression Right { get; internal init; }

        public override TypeSymbol ResultType => Operator.ResultType;
        public override bool Constant => Left.Constant && Right.Constant;
    }

    public partial class Binder
    {
        private BoundBinaryExpression BindBinaryExpression(BinaryExpression binaryExpression)
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var boundLeft = BindExpression(binaryExpression.Left);
            var boundRight = BindExpression(binaryExpression.Right);
            var boundBinaryOperator = BoundBinaryExpression.MatchBinaryOperator(boundLeft.ResultType, boundRight.ResultType, binaryExpression.Operator.Kind);

            if (boundBinaryOperator == null)
            {
                diagnosticBuilder.Add(
                    new Diagnostic()
                    {
                        Message = $"Operator {binaryExpression.Operator.Text} is not supported on types {boundLeft.ResultType.Name} and {boundRight.ResultType.Name}",
                        Level = DiagnosticLevel.Error,
                        TextLocation = binaryExpression.Operator.GetTextLocation(),
                        ErrorCode = ErrorCode.UnsupportedOperator
                    });
            }

            return BoundNodeFactory.CreateBoundBinaryExpression(
                syntaxNode: binaryExpression,
                left: boundLeft,
                right: boundRight,
                @operator: boundBinaryOperator,
                diagnosticBuilder: diagnosticBuilder);
        }
    }
}
