using System.Collections.Generic;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class BinaryExpression : Expression
    {
        public Expression Left { get; }
        public Expression Right { get; }
        public SyntaxToken Operator { get; }

        public BinaryExpression(
            SyntaxTree syntaxTree,
            Expression left,
            SyntaxToken operatorToken,
            Expression right) : base(syntaxTree)
        {
            this.Left = left;
            this.Operator = operatorToken;
            this.Right = right;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Left;
            yield return Operator;
            yield return Right;
        }
    }

    public sealed partial class Parser
    {
        internal Expression ParseBinaryExpression(int parentPrecedence = 0)
        {
            Expression left;
            var unaryPrecedence = SyntaxFacts.UnaryOperatorPrecedence.GetValueOrDefault(Current.Kind, 0);
            if (unaryPrecedence == 0 || unaryPrecedence <= parentPrecedence)
            {
                left = this.ParsePrimaryExpression();
            }
            else
            {
                var operatorToken = ExpectToken(Current.Kind);
                left = new UnaryExpression(this.syntaxTree, operatorToken, this.ParsePrimaryExpression(), false);
            }

            while (true)
            {
                var binaryPrecedence = SyntaxFacts.BinaryOperatorPrecedence.GetValueOrDefault(Current.Kind, 0);
                if (binaryPrecedence == 0 || binaryPrecedence <= parentPrecedence)
                {
                    break;
                }

                var operatorToken = this.NextToken();
                var right = ParseBinaryExpression(binaryPrecedence);

                left = new BinaryExpression(this.syntaxTree, left, operatorToken, right);
            }

            return left;
        }
    }
}
