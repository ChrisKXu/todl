using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class BlockStatement : Statement
    {
        public SyntaxToken OpenBraceToken { get; }
        public SyntaxToken CloseBraceToken { get; }
        public IReadOnlyList<Statement> InnerStatements { get; }

        public BlockStatement(
            SyntaxTree syntaxTree,
            SyntaxToken openBraceToken,
            IReadOnlyList<Statement> innerStatements,
            SyntaxToken closeBraceToken)
            : base(syntaxTree)
        {
            this.OpenBraceToken = openBraceToken;
            this.InnerStatements = innerStatements;
            this.CloseBraceToken = closeBraceToken;
        }

        public override TextSpan Text
            => TextSpan.FromTextSpans(OpenBraceToken.Text, CloseBraceToken.Text);

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return this.OpenBraceToken;
            foreach (var statement in this.InnerStatements)
            {
                yield return statement;
            }
            yield return this.CloseBraceToken;
        }
    }

    public sealed partial class Parser
    {
        private BlockStatement ParseBlockStatement()
        {
            var openBraceToken = this.ExpectToken(SyntaxKind.OpenBraceToken);
            var innerStatements = new List<Statement>();

            while (Current.Kind != SyntaxKind.CloseBraceToken)
            {
                innerStatements.Add(ParseStatement());
            }

            var closeBraceToken = this.ExpectToken(SyntaxKind.CloseBraceToken);

            return new BlockStatement(
                syntaxTree: this.syntaxTree,
                openBraceToken: openBraceToken,
                innerStatements: innerStatements,
                closeBraceToken: closeBraceToken);
        }
    }
}
