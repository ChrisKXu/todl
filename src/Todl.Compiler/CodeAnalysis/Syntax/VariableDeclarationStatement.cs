using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    // In Todl variables can be declared using either 'const' or 'let' modifier keyword
    // 'const' means readonly and 'let' means writable, simple as that
    public sealed class VariableDeclarationStatement : Statement
    {
        // const or let
        public SyntaxToken DeclarationKeyword { get; internal init; }
        public SyntaxToken IdentifierToken { get; internal init; }
        public SyntaxToken AssignmentToken { get; internal init; }
        public Expression InitializerExpression { get; internal init; }
        public SyntaxToken SemicolonToken { get; internal init; }

        public override TextSpan Text => TextSpan.FromTextSpans(DeclarationKeyword.Text, SemicolonToken.Text);

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return DeclarationKeyword;
            yield return IdentifierToken;
            yield return AssignmentToken;
            yield return InitializerExpression;
            yield return SemicolonToken;
        }
    }

    public sealed partial class Parser
    {
        private VariableDeclarationStatement ParseVariableDeclarationStatement()
        {
            var constOrLetKeyword = this.ExpectToken(Current.Kind);
            var identifierToken = this.ExpectToken(SyntaxKind.IdentifierToken);
            var equalsToken = this.ExpectToken(SyntaxKind.EqualsToken);
            var initializerExpression = this.ParseExpression();
            var semicolonToken = this.ExpectToken(SyntaxKind.SemicolonToken);

            return new VariableDeclarationStatement()
            {
                SyntaxTree = syntaxTree,
                DeclarationKeyword = constOrLetKeyword,
                IdentifierToken = identifierToken,
                AssignmentToken = equalsToken,
                InitializerExpression = initializerExpression,
                SemicolonToken = semicolonToken
            };
        }
    }
}
