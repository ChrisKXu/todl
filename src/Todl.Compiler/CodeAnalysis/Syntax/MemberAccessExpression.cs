using System;
using System.Collections.Generic;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class MemberAccessExpression : Expression
    {
        public Expression BaseExpression { get; }
        public SyntaxToken DotToken { get; }
        public SyntaxToken MemberIdentifierToken { get; }

        public string QualifiedName
        {
            get
            {
                return BaseExpression switch
                {
                    NameExpression nameExpression => $"{nameExpression.QualifiedName}.{MemberIdentifierToken.Text}",
                    MemberAccessExpression memberAccessExpression => $"{memberAccessExpression.QualifiedName}.{MemberIdentifierToken.Text}",
                    _ => throw new NotSupportedException()
                };
            }
        }

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
