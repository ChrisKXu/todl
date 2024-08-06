using System;
using System.Collections.Immutable;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class CommaSeparatedSyntaxList<T> : SyntaxNode where T : SyntaxNode
{
    public SyntaxToken OpenParenthesisToken { get; internal init; }
    public ImmutableArray<T> Items { get; internal init; }
    public SyntaxToken CloseParenthesisToken { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromTextSpans(OpenParenthesisToken.Text, CloseParenthesisToken.Text);
}

public sealed partial class Parser
{
    private CommaSeparatedSyntaxList<T> ParseCommaSeparatedSyntaxList<T>(Func<T> parseFunc) where T : SyntaxNode
    {
        var openParenthesisToken = ExpectToken(SyntaxKind.OpenParenthesisToken);
        var items = ImmutableArray.CreateBuilder<T>();

        var closeParenthesisToken = ExpectUntil(SyntaxKind.CloseParenthesisToken, () =>
        {
            if (Current.Kind == SyntaxKind.CommaToken)
            {
                ExpectToken(SyntaxKind.CommaToken);
            }

            var item = parseFunc();
            items.Add(item);
        });

        return new()
        {
            SyntaxTree = syntaxTree,
            OpenParenthesisToken = openParenthesisToken,
            Items = items.ToImmutable(),
            CloseParenthesisToken = closeParenthesisToken
        };
    }
}
