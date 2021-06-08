using System.Collections.Generic;

namespace Todl.CodeAnalysis
{
    public sealed class ParethesizedExpression : Expression
    {
        public SyntaxToken LeftParenthesisToken { get; }

        public Expression InnerExpression { get; }

        public SyntaxToken RightParenthesisToken { get; }

        public ParethesizedExpression(
            SyntaxTree syntaxTree,
            SyntaxToken leftParenthesisToken,
            Expression innerExpression,
            SyntaxToken rightParenthesisToken)
            : base(syntaxTree)
        {
            this.LeftParenthesisToken = leftParenthesisToken;
            this.InnerExpression = innerExpression;
            this.RightParenthesisToken = rightParenthesisToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return this.LeftParenthesisToken;
            yield return this.InnerExpression;
            yield return this.RightParenthesisToken;
        }
    }
}
