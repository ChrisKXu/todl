namespace Todl.Compiler.CodeAnalysis.Binding;

internal sealed class ConstantFoldingBoundNodeVisitor : BoundNodeVisitor
{
    public override BoundExpression VisitExpression(BoundExpression boundExpression)
    {
        if (!boundExpression.Constant)
        {
            return boundExpression;
        }

        return base.VisitExpression(boundExpression);
    }
}
