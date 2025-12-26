using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class UnaryExpression : Expression
{
    public SyntaxToken Operator { get; internal init; }
    public Expression Operand { get; internal init; }

    public override TextSpan Text => TextSpan.FromTextSpans(Operator.Text, Operand.Text);
}
