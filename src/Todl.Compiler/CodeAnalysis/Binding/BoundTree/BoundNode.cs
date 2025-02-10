using System.Diagnostics;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[DebuggerDisplay($"{nameof(GetDebuggerDisplay)}(),nq")]
internal abstract class BoundNode
{
    public SyntaxNode SyntaxNode { get; internal init; }

    public abstract BoundNode Accept(BoundTreeVisitor visitor);

    private string GetDebuggerDisplay()
    {
        return SyntaxNode == null ? GetType().Name : SyntaxNode.Text.ToString();
    }
}
