using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class BreakStatement : Statement
{
    public SyntaxToken BreakKeywordToken { get; internal init; }
    public NameExpression Label { get; internal init; }
    public SyntaxToken SemicolonToken { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromBounds(BreakKeywordToken.Span.Start, SemicolonToken.Span.End);
}

public sealed partial class Parser
{
    private BreakStatement ParseBreakStatement()
    {
        var breakKeywordToken = ExpectToken(SyntaxKind.BreakKeywordToken);
        var label = Current.Kind == SyntaxKind.IdentifierToken
            ? ParseLoopLabel()
            : null;

        return new()
        {
            SyntaxTree = syntaxTree,
            BreakKeywordToken = breakKeywordToken,
            Label = label,
            SemicolonToken = ExpectToken(SyntaxKind.SemicolonToken)
        };
    }
}
