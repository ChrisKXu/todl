using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    // In Todl variables can be declared using either 'const' or 'let' modifier keyword
    // 'const' means readonly and 'let' means writable, simple as that
    public sealed class VariableDeclarationStatement : Statement
    {
        // const or let
        public SyntaxToken DeclarationKeyword { get; internal init; }
        public TypeExpression TypeExpression { get; internal init; }
        public SyntaxToken IdentifierToken { get; internal init; }
        public SyntaxToken AssignmentToken { get; internal init; }
        public Expression InitializerExpression { get; internal init; }
        public SyntaxToken SemicolonToken { get; internal init; }

        public override TextSpan Text
            => TextSpan.FromBounds(DeclarationKeyword.Span.Start, SemicolonToken.Span.End);
    }

    public sealed partial class Parser
    {
        private VariableDeclarationStatement ParseVariableDeclarationStatement()
        {
            var constOrLetKeyword = this.ExpectToken(Current.Kind);

            // A type precedes the name when it starts with a type keyword, or an identifier not followed by '=' ('let uint = 1;' names a variable).
            var typeExpression = SyntaxFacts.BuiltInTypes.Contains(Current.Kind)
                || (Current.Kind == SyntaxKind.IdentifierToken && Peak.Kind != SyntaxKind.EqualsToken)
                ? this.ParseTypeExpression()
                : null;

            var identifierToken = this.ExpectToken(SyntaxKind.IdentifierToken);
            var equalsToken = this.ExpectToken(SyntaxKind.EqualsToken);
            var initializerExpression = this.ParseExpression();
            var semicolonToken = this.ExpectToken(SyntaxKind.SemicolonToken);

            return new VariableDeclarationStatement()
            {
                SyntaxTree = syntaxTree,
                DeclarationKeyword = constOrLetKeyword,
                TypeExpression = typeExpression,
                IdentifierToken = identifierToken,
                AssignmentToken = equalsToken,
                InitializerExpression = initializerExpression,
                SemicolonToken = semicolonToken
            };
        }
    }
}
