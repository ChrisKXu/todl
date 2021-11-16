using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class BinaryExpression : Expression
    {
        public Expression Left { get; internal init; }
        public Expression Right { get; internal init; }
        public SyntaxToken Operator { get; internal init; }

        public override TextSpan Text => TextSpan.FromTextSpans(Left.Text, Right.Text);
    }

    public sealed partial class Parser
    {
        private Expression ParseBinaryExpression(int parentPrecedence = 0)
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
                left = new UnaryExpression()
                {
                    SyntaxTree = syntaxTree,
                    Operator = operatorToken,
                    Operand = ParsePrimaryExpression(),
                    Trailing = false
                };
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

                left = new BinaryExpression()
                {
                    SyntaxTree = syntaxTree,
                    Left = left,
                    Right = right,
                    Operator = operatorToken
                };
            }

            return left;
        }
    }
}
