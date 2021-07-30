using System.Collections.Generic;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class FunctionCallExpression : Expression
    {
        public FunctionCallExpression(SyntaxTree syntaxTree)
            : base(syntaxTree)
        {
        }

        public Expression BaseExpression { get; internal init; }
        public SyntaxToken OpenParenthesisToken { get; internal init; }
        public SyntaxToken CloseParenthesisToken { get; internal init; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return BaseExpression;
            yield return OpenParenthesisToken;
            yield return CloseParenthesisToken;
        }
    }

    public sealed partial class Parser
    {
        private FunctionCallExpression ParseFunctionCallExpression(Expression baseExpression)
        {
            var openParenthesisToken = ExpectToken(SyntaxKind.OpenParenthesisToken);

            while (Current.Kind != SyntaxKind.CloseParenthesisToken)
            {

            }

            var closeParenthesisToken = ExpectToken(SyntaxKind.CloseParenthesisToken);

            return new FunctionCallExpression(this.syntaxTree)
            {
                BaseExpression = baseExpression,
                OpenParenthesisToken = openParenthesisToken,
                CloseParenthesisToken = closeParenthesisToken
            };
        }
    }
}
