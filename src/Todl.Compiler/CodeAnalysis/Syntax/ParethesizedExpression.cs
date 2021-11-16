using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class ParethesizedExpression : Expression
    {
        public SyntaxToken LeftParenthesisToken { get; internal init; }
        public Expression InnerExpression { get; internal init; }
        public SyntaxToken RightParenthesisToken { get; internal init; }

        public override TextSpan Text => TextSpan.FromTextSpans(LeftParenthesisToken.Text, RightParenthesisToken.Text);

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
            => new()
            {
                SyntaxTree = syntaxTree,
                LeftParenthesisToken = ExpectToken(SyntaxKind.OpenParenthesisToken),
                InnerExpression = ParseBinaryExpression(),
                RightParenthesisToken = ExpectToken(SyntaxKind.CloseParenthesisToken)
            };
    }
}
