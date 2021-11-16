using System;
using System.Collections.Generic;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class MemberAccessExpression : Expression
    {
        public Expression BaseExpression { get; internal init; }
        public SyntaxToken DotToken { get; internal init; }
        public SyntaxToken MemberIdentifierToken { get; internal init; }

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

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return BaseExpression;
            yield return DotToken;
            yield return MemberIdentifierToken;
        }
    }
}
