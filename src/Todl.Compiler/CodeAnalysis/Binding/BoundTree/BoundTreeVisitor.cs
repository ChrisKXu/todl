using System.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

internal abstract partial class BoundTreeVisitor
{
    [DebuggerHidden]
    public virtual BoundNode DefaultVisit(BoundNode node) => default;

    [DebuggerHidden]
    public virtual BoundNode Visit(BoundNode node) => node?.Accept(this);
}
