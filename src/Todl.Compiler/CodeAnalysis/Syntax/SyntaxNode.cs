using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public abstract class SyntaxNode
{
    public SyntaxTree SyntaxTree { get; internal init; }
    public abstract TextSpan Text { get; }
}
