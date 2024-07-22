using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundExpressionStatement : BoundStatement
{
    public BoundExpression Expression { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundExpressionStatement(this);
}

public partial class Binder
{
    private BoundExpressionStatement BindExpressionStatement(ExpressionStatement expressionStatement)
        => BoundNodeFactory.CreateBoundExpressionStatement(
            syntaxNode: expressionStatement,
            expression: BindExpression(expressionStatement.Expression));
}
