using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class TypeExpression : Expression
{
    public NameExpression BaseTypeExpression { get; internal init; }
    public IReadOnlyList<ArrayRankSpecifier> ArrayRankSpecifiers { get; internal init; }

    public bool IsArrayType => ArrayRankSpecifiers.Any();

    public override TextSpan Text
        => !IsArrayType
            ? BaseTypeExpression.Text
            : TextSpan.FromTextSpans(BaseTypeExpression.Text, ArrayRankSpecifiers[^1].CloseBracketToken.Text);
}

public sealed class ArrayRankSpecifier : SyntaxNode
{
    public SyntaxToken OpenBracketToken { get; internal set; }
    public SyntaxToken CloseBracketToken { get; internal set; }

    public override TextSpan Text => TextSpan.FromTextSpans(OpenBracketToken.Text, CloseBracketToken.Text);
}

public sealed partial class Parser
{
    private TypeExpression ParseTypeExpression()
    {
        var nameExpression = ParseNameExpression();
        var arrayRankSpecifiers = new List<ArrayRankSpecifier>();

        while (Current.Kind == SyntaxKind.OpenBracketToken)
        {
            arrayRankSpecifiers.Add(ParseArrayRankSpecifier());
        }

        return new()
        {
            BaseTypeExpression = nameExpression,
            ArrayRankSpecifiers = arrayRankSpecifiers
        };
    }

    private ArrayRankSpecifier ParseArrayRankSpecifier()
        => new()
        {
            OpenBracketToken = ExpectToken(SyntaxKind.OpenBracketToken),
            CloseBracketToken = ExpectToken(SyntaxKind.CloseBracketToken)
        };
}
