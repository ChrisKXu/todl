using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class MemberAccessExpression : Expression
{
    public Expression BaseExpression { get; internal init; }
    public SyntaxToken DotToken { get; internal init; }
    public SyntaxToken MemberIdentifierToken { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromBounds(BaseExpression.Text.Start, MemberIdentifierToken.Span.End);
}
