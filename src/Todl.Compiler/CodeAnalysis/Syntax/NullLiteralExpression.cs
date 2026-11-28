using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class NullLiteralExpression : Expression
{
    public SyntaxToken NullKeywordToken { get; internal init; }
    public override TextSpan Text => NullKeywordToken.Span;
}

public sealed partial class Parser
{
    private NullLiteralExpression ParseNullLiteralExpression()
        => new()
        {
            SyntaxTree = syntaxTree,
            NullKeywordToken = ExpectToken(SyntaxKind.NullKeywordToken)
        };
}
