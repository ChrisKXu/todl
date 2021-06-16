using System.Collections.Generic;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class UnaryExpression : Expression
    {
        public SyntaxToken Operator { get; }
        public Expression Operand { get; }
        public bool Inversed { get; }

        public UnaryExpression(
            SyntaxTree syntaxTree,
            SyntaxToken operatorToken,
            Expression operand,
            bool inversed) : base(syntaxTree)
        {
            this.Operator = operatorToken;
            this.Operand = operand;
            this.Inversed = inversed;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            if (this.Inversed)
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