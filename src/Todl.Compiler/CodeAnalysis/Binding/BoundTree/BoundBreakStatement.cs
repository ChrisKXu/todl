using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundBreakStatement : BoundStatement
{
    public BoundLoopContext BoundLoopContext { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundBreakStatement(this);
}

public partial class Binder
{
    private BoundBreakStatement BindBreakStatement(BreakStatement breakStatement)
    {
        var boundLoopContext = ResolveLoopContext(breakStatement.Label, breakStatement.GetTextLocation());
        return BoundNodeFactory.CreateBoundBreakStatement(breakStatement, boundLoopContext);
    }
}
