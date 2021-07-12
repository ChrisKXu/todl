using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundExpressionStatement : BoundStatement
    {
        public BoundExpression Expression { get; }

        public BoundExpressionStatement(BoundExpression expression)
        {
            this.Expression = expression;
        }
    }

    public sealed partial class Binder
    {
        private BoundExpressionStatement BindExpressionStatement(BoundScope scope, ExpressionStatement expressionStatement)
        {
            return new BoundExpressionStatement(this.BindExpression(scope, expressionStatement.Expression));
        }
    }
}
