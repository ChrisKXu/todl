using System.Collections.Generic;
using System.Linq;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class UnaryExpression : Expression
    {
        public SyntaxToken Operator { get; }
        public Expression Operand { get; }
        public bool Trailing { get; }

        public UnaryExpression(
            SyntaxTree syntaxTree,
            SyntaxToken operatorToken,
            Expression operand,
            bool trailing) : base(syntaxTree)
        {
            this.Operator = operatorToken;
            this.Operand = operand;
            this.Trailing = trailing;
        }

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
}