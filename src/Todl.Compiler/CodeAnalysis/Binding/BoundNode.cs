using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding;

public abstract class BoundNode
{
    public SyntaxNode SyntaxNode { get; internal init; }
}
