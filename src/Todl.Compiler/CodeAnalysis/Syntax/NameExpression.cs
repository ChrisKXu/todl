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

    /// <summary>
    /// Returns the name with :: replaced by . for CLR type lookup.
    /// e.g., "System::Console" becomes "System.Console"
    /// </summary>
    public string CanonicalName
    {
        get
        {
            if (IsSimpleName)
                return SyntaxTokens[0].Text.ToString();

            var builder = new StringBuilder();
            foreach (var token in SyntaxTokens)
            {
                if (token.Kind == SyntaxKind.ColonColonToken)
                    builder.Append('.');
                else
                    builder.Append(token.Text);
            }
            return builder.ToString();
        }
    }
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

        // Use :: for namespace qualification - no ClrTypeCache check needed
        while (Current.Kind == SyntaxKind.ColonColonToken && Peak.Kind == SyntaxKind.IdentifierToken)
        {
            syntaxTokens.Add(ExpectToken(SyntaxKind.ColonColonToken));
            syntaxTokens.Add(ExpectToken(SyntaxKind.IdentifierToken));
        }

        return new()
        {
            SyntaxTree = syntaxTree,
            SyntaxTokens = syntaxTokens.ToImmutable()
        };
    }
}
