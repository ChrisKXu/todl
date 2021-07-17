using System.Collections.Generic;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class MemberAccessExpression : Expression
    {
        public Expression BaseExpression { get; }
        public SyntaxToken DotToken { get; }
        public SyntaxToken MemberIdentifierToken { get; }

        public MemberAccessExpression(
            SyntaxTree syntaxTree,
            Expression baseExpression,
            SyntaxToken dotToken,
            SyntaxToken memberIdentifierToken)
            : base(syntaxTree)
        {
            this.BaseExpression = baseExpression;
            this.DotToken = dotToken;
            this.MemberIdentifierToken = memberIdentifierToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return BaseExpression;
            yield return DotToken;
            yield return MemberIdentifierToken;
        }
    }
}
