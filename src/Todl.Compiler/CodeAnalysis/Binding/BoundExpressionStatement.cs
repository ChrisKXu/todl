using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundExpressionStatement : BoundStatement
    {
        public BoundExpression Expression { get; internal init; }
    }

    public sealed partial class Binder
    {
        private BoundExpressionStatement BindExpressionStatement(BoundScope scope, ExpressionStatement expressionStatement)
            => new()
            {
                SyntaxNode = expressionStatement,
                Expression = BindExpression(scope, expressionStatement.Expression)
            };
    }
}
