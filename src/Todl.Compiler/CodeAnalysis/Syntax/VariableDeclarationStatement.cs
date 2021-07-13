using System.Collections.Generic;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    // In Todl variables can be declared using either 'const' or 'let' modifier keyword
    // 'const' means readonly and 'let' means writable, simple as that
    public sealed class VariableDeclarationStatement : Statement
    {
        // const or let
        public SyntaxToken DeclarationKeyword { get; }
        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken AssignmentToken { get; }
        public Expression InitializerExpression { get; }
        public SyntaxToken SemicolonToken { get; }

        public VariableDeclarationStatement(
            SyntaxTree syntaxTree,
            SyntaxToken declarationKeyword,
            SyntaxToken identifierToken,
            SyntaxToken assignmentToken,
            Expression initializerExpression,
            SyntaxToken semicolonToken) : base(syntaxTree)
        {
            this.DeclarationKeyword = declarationKeyword;
            this.IdentifierToken = identifierToken;
            this.AssignmentToken = assignmentToken;
            this.InitializerExpression = initializerExpression;
            this.SemicolonToken = semicolonToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return this.DeclarationKeyword;
            yield return this.IdentifierToken;
            yield return this.AssignmentToken;
            yield return this.InitializerExpression;
            yield return this.SemicolonToken;
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

            return new VariableDeclarationStatement(
                syntaxTree: this.syntaxTree,
                declarationKeyword: constOrLetKeyword,
                identifierToken: identifierToken,
                assignmentToken: equalsToken,
                initializerExpression: initializerExpression,
                semicolonToken: semicolonToken);
        }
    }
}
