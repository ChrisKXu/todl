using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundContinueStatement : BoundStatement
{
    public BoundLoopContext BoundLoopContext { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundContinueStatement(this);
}

public partial class Binder
{
    private BoundContinueStatement BindContinueStatement(ContinueStatement continueStatement)
    {
        var boundLoopContext = ResolveLoopContext(continueStatement.Label, continueStatement.GetTextLocation());
        return BoundNodeFactory.CreateBoundContinueStatement(continueStatement, boundLoopContext);
    }
}
