using System.Collections.Immutable;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class TypeExpression : Expression
{
    public NameExpression BaseTypeExpression { get; internal init; }
    public ImmutableArray<ArrayRankSpecifier> ArrayRankSpecifiers { get; internal init; }

    public bool IsArrayType => ArrayRankSpecifiers.Any();

    public override TextSpan Text
        => !IsArrayType
            ? BaseTypeExpression.Text
            : TextSpan.FromBounds(BaseTypeExpression.Text.Start, ArrayRankSpecifiers[^1].CloseBracketToken.Span.End);
}

public sealed class ArrayRankSpecifier : SyntaxNode
{
    public SyntaxToken OpenBracketToken { get; internal set; }
    public SyntaxToken CloseBracketToken { get; internal set; }

    public override TextSpan Text => TextSpan.FromBounds(OpenBracketToken.Span.Start, CloseBracketToken.Span.End);
}

public sealed partial class Parser
{
    private TypeExpression ParseTypeExpression()
    {
        var nameExpression = ParseNameExpression();
        var arrayRankSpecifiers = ImmutableArray.CreateBuilder<ArrayRankSpecifier>();

        while (Current.Kind == SyntaxKind.OpenBracketToken)
        {
            arrayRankSpecifiers.Add(ParseArrayRankSpecifier());
        }

        return new()
        {
            SyntaxTree = syntaxTree,
            BaseTypeExpression = nameExpression,
            ArrayRankSpecifiers = arrayRankSpecifiers.ToImmutable()
        };
    }

    private ArrayRankSpecifier ParseArrayRankSpecifier()
        => new()
        {
            SyntaxTree = syntaxTree,
            OpenBracketToken = ExpectToken(SyntaxKind.OpenBracketToken),
            CloseBracketToken = ExpectToken(SyntaxKind.CloseBracketToken)
        };
}
