using System.Collections.Generic;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class AssignmentExpression : Expression
    {
        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken EqualsToken { get; }
        public Expression Expression { get; }

        public AssignmentExpression(
            SyntaxTree syntaxTree,
            SyntaxToken identifierToken,
            SyntaxToken equalsToken,
            Expression expression)
            : base(syntaxTree)
        {
            this.IdentifierToken = identifierToken;
            this.EqualsToken = equalsToken;
            this.Expression = expression;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return this.IdentifierToken;
            yield return this.EqualsToken;
            yield return this.Expression;
        }
    }

    public sealed partial class Parser
    {
        public AssignmentExpression ParseAssignmentExpression()
        {
            var identifierToken = this.ExpectToken(SyntaxKind.IdentifierToken);
            var equalsToken = this.ExpectToken(SyntaxKind.EqualsToken);
            var expression = this.ParseExpression();

            return new AssignmentExpression(
                syntaxTree: this.syntaxTree,
                identifierToken: identifierToken,
                equalsToken: equalsToken,
                expression: expression);
        }
    }
}
