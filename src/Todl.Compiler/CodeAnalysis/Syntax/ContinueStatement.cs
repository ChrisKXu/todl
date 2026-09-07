using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class ContinueStatement : Statement
{
    public SyntaxToken ContinueKeywordToken { get; internal init; }
    public NameExpression Label { get; internal init; }
    public SyntaxToken SemicolonToken { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromBounds(ContinueKeywordToken.Span.Start, SemicolonToken.Span.End);
}

public sealed partial class Parser
{
    private ContinueStatement ParseContinueStatement()
    {
        var continueKeywordToken = ExpectToken(SyntaxKind.ContinueKeywordToken);
        var label = Current.Kind == SyntaxKind.IdentifierToken
            ? ParseLoopLabel()
            : null;

        return new()
        {
            SyntaxTree = syntaxTree,
            ContinueKeywordToken = continueKeywordToken,
            Label = label,
            SemicolonToken = ExpectToken(SyntaxKind.SemicolonToken)
        };
    }
}
