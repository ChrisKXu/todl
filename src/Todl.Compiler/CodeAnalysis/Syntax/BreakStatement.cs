using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class BreakStatement : Statement
{
    public SyntaxToken BreakKeywordToken { get; internal init; }
    public SyntaxToken SemicolonToken { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromBounds(BreakKeywordToken.Span.Start, SemicolonToken.Span.End);
}

public sealed partial class Parser
{
    private BreakStatement ParseBreakStatement()
    {
        return new()
        {
            SyntaxTree = syntaxTree,
            BreakKeywordToken = ExpectToken(SyntaxKind.BreakKeywordToken),
            SemicolonToken = ExpectToken(SyntaxKind.SemicolonToken)
        };
    }
}
