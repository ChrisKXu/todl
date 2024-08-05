using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class UnaryExpression : Expression
{
    public SyntaxToken Operator { get; internal init; }
    public Expression Operand { get; internal init; }
    public bool Trailing { get; internal init; }

    public override TextSpan Text
    {
        get
        {
            if (Trailing)
            {
                return TextSpan.FromTextSpans(Operand.Text, Operator.Text);
            }

            return TextSpan.FromTextSpans(Operator.Text, Operand.Text);
        }
    }
}

public sealed partial class Parser
{
    private Expression ParseTrailingUnaryExpression(Expression expression)
    {
        if (Current.Kind == SyntaxKind.PlusPlusToken || Current.Kind == SyntaxKind.MinusMinusToken)
        {
            return new UnaryExpression()
            {
                SyntaxTree = syntaxTree,
                Operator = ExpectToken(Current.Kind),
                Operand = expression,
                Trailing = true
            };
        }

        return expression;
    }
}
