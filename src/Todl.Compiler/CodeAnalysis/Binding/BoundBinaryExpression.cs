using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

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
                this.SyntaxKind = syntaxKind;
                this.BoundBinaryOperatorKind = boundBinaryOperatorKind;
                this.ResultType = resultType;
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
            { Tuple.Create(TypeSymbol.ClrInt32, TypeSymbol.ClrInt32, SyntaxKind.PlusToken), new BoundBinaryOperator(SyntaxKind.PlusToken, BoundBinaryOperatorKind.NumericAddition, TypeSymbol.ClrInt32) },
            { Tuple.Create(TypeSymbol.ClrInt32, TypeSymbol.ClrInt32, SyntaxKind.MinusToken), new BoundBinaryOperator(SyntaxKind.MinusToken, BoundBinaryOperatorKind.NumericSubstraction, TypeSymbol.ClrInt32) },
            { Tuple.Create(TypeSymbol.ClrInt32, TypeSymbol.ClrInt32, SyntaxKind.StarToken), new BoundBinaryOperator(SyntaxKind.StarToken, BoundBinaryOperatorKind.NumericMultiplication, TypeSymbol.ClrInt32) },
            { Tuple.Create(TypeSymbol.ClrInt32, TypeSymbol.ClrInt32, SyntaxKind.SlashToken), new BoundBinaryOperator(SyntaxKind.SlashToken, BoundBinaryOperatorKind.NumericDivision, TypeSymbol.ClrInt32) },

            { Tuple.Create(TypeSymbol.ClrBoolean, TypeSymbol.ClrBoolean, SyntaxKind.AmpersandAmpersandToken), new BoundBinaryOperator(SyntaxKind.AmpersandAmpersandToken, BoundBinaryOperatorKind.LogicalAnd, TypeSymbol.ClrBoolean) },
            { Tuple.Create(TypeSymbol.ClrBoolean, TypeSymbol.ClrBoolean, SyntaxKind.PipePipeToken), new BoundBinaryOperator(SyntaxKind.PipePipeToken, BoundBinaryOperatorKind.LogicalOr, TypeSymbol.ClrBoolean) },

            { Tuple.Create(TypeSymbol.ClrString, TypeSymbol.ClrString, SyntaxKind.PlusToken), new BoundBinaryOperator(SyntaxKind.PlusToken, BoundBinaryOperatorKind.StringConcatenation, TypeSymbol.ClrString) }
        };

        public static BoundBinaryOperator MatchBinaryOperator(TypeSymbol leftResultType, TypeSymbol rightResultType, SyntaxKind syntaxKind)
            => BoundBinaryExpression.supportedBinaryOperators.GetValueOrDefault(Tuple.Create(leftResultType, rightResultType, syntaxKind), null);

        public BoundBinaryOperator Operator { get; }
        public BoundExpression Left { get; }
        public BoundExpression Right { get; }
        public override TypeSymbol ResultType => this.Operator.ResultType;

        public BoundBinaryExpression(BoundBinaryOperator boundBinaryOperator, BoundExpression left, BoundExpression right)
        {
            this.Operator = boundBinaryOperator;
            this.Left = left;
            this.Right = right;
        }
    }
}
