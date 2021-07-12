using System.Collections.Generic;

namespace Todl.Compiler.CodeAnalysis.Syntax
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

    public sealed partial class Parser
    {
        private ParethesizedExpression ParseParethesizedExpression()
        {
            var leftParenthesisToken = this.ExpectToken(SyntaxKind.OpenParenthesisToken);
            var innerExpression = ParseBinaryExpression();
            var rightParenthesisToken = this.ExpectToken(SyntaxKind.CloseParenthesisToken);

            return new ParethesizedExpression(this.syntaxTree, leftParenthesisToken, innerExpression, rightParenthesisToken);
        }
    }
}
