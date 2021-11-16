using System.Collections.Generic;
using System.Linq;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class UnaryExpression : Expression
    {
        public SyntaxToken Operator { get; internal init; }
        public Expression Operand { get; internal init; }
        public bool Trailing { get; internal init; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            if (this.Trailing)
            {
                yield return Operand;
                yield return Operator;
            }
            else
            {
                yield return Operator;
                yield return Operand;
            }
        }
    }

    public sealed partial class Parser
    {
        private Expression ParseTrailingUnaryExpression(Expression expression)
        {
            if (Current.Kind == SyntaxKind.PlusPlusToken || Current.Kind == SyntaxKind.MinusMinusToken)
            {
                return new UnaryExpression()
                {
                    SyntaxTree = syntaxTree,
                    Operator = ExpectToken(Current.Kind),
                    Operand = expression,
                    Trailing = true
                };
            }

            return expression;
        }
    }
}
