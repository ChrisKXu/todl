using System.Collections.Generic;

namespace Todl.Compiler.CodeAnalysis
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
}