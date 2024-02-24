using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class ContinueStatement : Statement
{
    public SyntaxToken ContinueKeywordToken { get; internal init; }
    public SyntaxToken SemicolonToken { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromTextSpans(ContinueKeywordToken.Text, SemicolonToken.Text);
}

public sealed partial class Parser
{
    private ContinueStatement ParseContinueStatement()
    {
        return new()
        {
            SyntaxTree = syntaxTree,
            ContinueKeywordToken = ExpectToken(SyntaxKind.ContinueKeywordToken),
            SemicolonToken = ExpectToken(SyntaxKind.SemicolonToken)
        };
    }
}
