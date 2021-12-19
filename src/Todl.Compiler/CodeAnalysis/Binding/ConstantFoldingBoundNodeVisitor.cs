namespace Todl.Compiler.CodeAnalysis.Binding;

internal sealed class ConstantFoldingBoundNodeVisitor : BoundNodeVisitor
{
    public override BoundExpression VisitBoundExpression(BoundExpression boundExpression)
    {
        if (!boundExpression.Constant)
        {
            return boundExpression;
        }

        return base.VisitBoundExpression(boundExpression);
    }
}
