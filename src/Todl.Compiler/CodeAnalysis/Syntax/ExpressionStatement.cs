using System.Collections.Generic;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class ExpressionStatement : Statement
    {
        public Expression Expression { get; }
        public SyntaxToken SemicolonToken { get; }

        public ExpressionStatement(SyntaxTree syntaxTree, Expression expression, SyntaxToken semicolonToken)
            : base(syntaxTree)
        {
            this.Expression = expression;
            this.SemicolonToken = semicolonToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return this.Expression;
            yield return this.SemicolonToken;
        }
    }

    public sealed partial class Parser
    {
        private ExpressionStatement ParseExpressionStatement()
        {
            return new ExpressionStatement(
                syntaxTree: syntaxTree,
                expression: this.ParseExpression(),
                semicolonToken: this.ExpectToken(SyntaxKind.SemicolonToken));
        }
    }
}
