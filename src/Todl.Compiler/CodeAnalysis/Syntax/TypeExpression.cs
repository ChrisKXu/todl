using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class TypeExpression : Expression
{
    public Expression BaseTypeExpression { get; internal init; }
    public SyntaxToken? OpenBracketToken { get; internal set; }
    public SyntaxToken? CloseBracketToken { get; internal set; }

    public bool IsArrayType => OpenBracketToken is not null;

    public override TextSpan Text
        => CloseBracketToken is not null
            ? TextSpan.FromTextSpans(BaseTypeExpression.Text, CloseBracketToken.Value.Text)
            : BaseTypeExpression.Text;
}

public sealed partial class Parser
{
    private TypeExpression ParseTypeExpression()
    {
        var nameExpression = ParseNameExpression();

        if (Current.Kind != SyntaxKind.OpenBracketToken)
        {
            return new()
            {
                SyntaxTree = syntaxTree,
                BaseTypeExpression = nameExpression
            };
        }

        var typeExpression = new TypeExpression()
        {
            SyntaxTree = syntaxTree,
            BaseTypeExpression = nameExpression,
            OpenBracketToken = ExpectToken(SyntaxKind.OpenBracketToken),
            CloseBracketToken = ExpectToken(SyntaxKind.CloseBracketToken)
        };

        while (Current.Kind == SyntaxKind.OpenBracketToken)
        {
            typeExpression = new()
            {
                SyntaxTree = syntaxTree,
                BaseTypeExpression = typeExpression,
                OpenBracketToken = ExpectToken(SyntaxKind.OpenBracketToken),
                CloseBracketToken = ExpectToken(SyntaxKind.CloseBracketToken)
            };
        }

        return typeExpression;
    }
}
