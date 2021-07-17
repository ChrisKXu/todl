using System.Collections.Generic;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class AssignmentExpression : Expression
    {
        public Expression Left { get; }
        public SyntaxToken AssignmentOperator { get; }
        public Expression Right { get; }

        public AssignmentExpression(
            SyntaxTree syntaxTree,
            Expression left,
            SyntaxToken assignmentOperator,
            Expression right)
            : base(syntaxTree)
        {
            this.Left = left;
            this.AssignmentOperator = assignmentOperator;
            this.Right = right;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return this.Left;
            yield return this.AssignmentOperator;
            yield return this.Right;
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
        private AssignmentExpression ParseAssignmentExpression(Expression left)
        {
            return new AssignmentExpression(
                syntaxTree: this.syntaxTree,
                left: left,
                assignmentOperator: ExpectToken(Current.Kind),
                right: ParseExpression());
        }
    }
}
