using System.Collections.Immutable;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class ArrayLiteralExpression : Expression
{
    public SyntaxToken OpenBracketToken { get; internal init; }
    public ImmutableArray<Expression> Elements { get; internal init; }
    public SyntaxToken CloseBracketToken { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromBounds(OpenBracketToken.Span.Start, CloseBracketToken.Span.End);
}

public sealed partial class Parser
{
    private ArrayLiteralExpression ParseArrayLiteralExpression()
    {
        var openBracketToken = ExpectToken(SyntaxKind.OpenBracketToken);
        var elements = ImmutableArray.CreateBuilder<Expression>();

        elements.Add(ParseExpression());

        var closeBracketToken = ExpectUntil(SyntaxKind.CloseBracketToken, () =>
        {
            if (Current.Kind == SyntaxKind.CommaToken)
            {
                ExpectToken(SyntaxKind.CommaToken);
                elements.Add(ParseExpression());
            }
        });

        return new()
        {
            SyntaxTree = syntaxTree,
            OpenBracketToken = openBracketToken,
            Elements = elements.ToImmutable(),
            CloseBracketToken = closeBracketToken
        };
    }
}
