using System.Collections.Immutable;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class NewArrayExpression : Expression
{
    public SyntaxToken NewKeywordToken { get; internal init; }
    public NameExpression ElementTypeNameExpression { get; internal init; }
    public SyntaxToken OpenBracketToken { get; internal init; }
    public Expression LengthExpression { get; internal init; }
    public SyntaxToken CloseBracketToken { get; internal init; }
    public ImmutableArray<ArrayRankSpecifier> ArrayRankSpecifiers { get; internal init; }

    public override TextSpan Text
        => TextSpan.FromBounds(
            NewKeywordToken.Span.Start,
            ArrayRankSpecifiers.IsEmpty ? CloseBracketToken.Span.End : ArrayRankSpecifiers[^1].CloseBracketToken.Span.End);
}

public sealed partial class Parser
{
    private NewArrayExpression ParseNewArrayExpression(SyntaxToken newKeywordToken, NameExpression elementTypeNameExpression)
    {
        var openBracketToken = ExpectToken(SyntaxKind.OpenBracketToken);
        var lengthExpression = ParseExpression();
        var closeBracketToken = ExpectToken(SyntaxKind.CloseBracketToken);

        var arrayRankSpecifiers = ImmutableArray.CreateBuilder<ArrayRankSpecifier>();
        while (Current.Kind == SyntaxKind.OpenBracketToken)
        {
            arrayRankSpecifiers.Add(ParseArrayRankSpecifier());
        }

        return new()
        {
            SyntaxTree = syntaxTree,
            NewKeywordToken = newKeywordToken,
            ElementTypeNameExpression = elementTypeNameExpression,
            OpenBracketToken = openBracketToken,
            LengthExpression = lengthExpression,
            CloseBracketToken = closeBracketToken,
            ArrayRankSpecifiers = arrayRankSpecifiers.ToImmutable()
        };
    }
}
