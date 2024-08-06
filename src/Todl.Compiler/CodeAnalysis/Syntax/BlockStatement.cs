using System.Collections.Immutable;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class BlockStatement : Statement
{
    public SyntaxToken OpenBraceToken { get; internal init; }
    public SyntaxToken CloseBraceToken { get; internal init; }
    public ImmutableArray<Statement> InnerStatements { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromTextSpans(OpenBraceToken.Text, CloseBraceToken.Text);
}

public sealed partial class Parser
{
    private BlockStatement ParseBlockStatement()
    {
        var openBraceToken = ExpectToken(SyntaxKind.OpenBraceToken);
        var innerStatements = ImmutableArray.CreateBuilder<Statement>();

        var closeBraceToken = ExpectUntil(SyntaxKind.CloseBraceToken, () =>
        {
            innerStatements.Add(ParseStatement());
        });

        return new()
        {
            SyntaxTree = syntaxTree,
            OpenBraceToken = openBraceToken,
            CloseBraceToken = closeBraceToken,
            InnerStatements = innerStatements.ToImmutable()
        };
    }
}
