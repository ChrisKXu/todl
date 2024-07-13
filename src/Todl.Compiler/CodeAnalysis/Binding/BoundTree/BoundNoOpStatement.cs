namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundNoOpStatement : BoundStatement
{
    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundNoOpStatement(this);
}
