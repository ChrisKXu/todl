using System.Collections.Generic;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class AssignmentExpression : Expression
    {
        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken AssignmentOperator { get; }
        public Expression Expression { get; }

        public AssignmentExpression(
            SyntaxTree syntaxTree,
            SyntaxToken identifierToken,
            SyntaxToken assignmentOperator,
            Expression expression)
            : base(syntaxTree)
        {
            this.IdentifierToken = identifierToken;
            this.AssignmentOperator = assignmentOperator;
            this.Expression = expression;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return this.IdentifierToken;
            yield return this.AssignmentOperator;
            yield return this.Expression;
        }

        public static readonly IReadOnlySet<SyntaxKind> AssignmentOperators = new HashSet<SyntaxKind>()
        {
            SyntaxKind.EqualsToken,
            SyntaxKind.PlusEqualsToken,
            SyntaxKind.MinusEqualsToken,
            SyntaxKind.StarEqualsToken,
            SyntaxKind.SlashEqualsToken
        };
    }

    public sealed partial class Parser
    {
        private AssignmentExpression ParseAssignmentExpression()
        {
            var identifierToken = this.ExpectToken(SyntaxKind.IdentifierToken);
            var assignmentOperator = this.ExpectToken(Current.Kind);
            var expression = this.ParseExpression();

            return new AssignmentExpression(
                syntaxTree: this.syntaxTree,
                identifierToken: identifierToken,
                assignmentOperator: assignmentOperator,
                expression: expression);
        }
    }
}
