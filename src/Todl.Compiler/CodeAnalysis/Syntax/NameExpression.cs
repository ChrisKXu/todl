using System.Collections.Immutable;
using System.Text;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class NameExpression : Expression
{
    public ImmutableArray<SyntaxToken> SyntaxTokens { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromTextSpans(SyntaxTokens[0].Text, SyntaxTokens[^1].Text);

    public bool IsSimpleName => SyntaxTokens.Length == 1;
}

public sealed partial class Parser
{
    private NameExpression ParseNameExpression()
    {
        if (SyntaxFacts.BuiltInTypes.Contains(Current.Kind))
        {
            return new()
            {
                SyntaxTree = syntaxTree,
                SyntaxTokens = ImmutableArray.CreateRange([ExpectToken(Current.Kind)])
            };
        }

        var syntaxTokens = ImmutableArray.CreateBuilder<SyntaxToken>();
        syntaxTokens.Add(ExpectToken(SyntaxKind.IdentifierToken));

        var builder = new StringBuilder(syntaxTokens[0].Text.ToString());

        while (Current.Kind == SyntaxKind.DotToken && Peak.Kind == SyntaxKind.IdentifierToken)
        {
            if (!syntaxTree.ClrTypeCache.Namespaces.Contains(builder.ToString()))
            {
                break;
            }

            syntaxTokens.Add(ExpectToken(SyntaxKind.DotToken));

            var identifierToken = ExpectToken(SyntaxKind.IdentifierToken);
            syntaxTokens.Add(identifierToken);
            builder.Append($".{identifierToken.Text}");
        }

        return new()
        {
            SyntaxTree = syntaxTree,
            SyntaxTokens = syntaxTokens.ToImmutable()
        };
    }
}
