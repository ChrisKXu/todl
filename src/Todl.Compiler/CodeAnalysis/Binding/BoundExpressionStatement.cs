using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    [BoundNode]
    public sealed class BoundExpressionStatement : BoundStatement
    {
        public BoundExpression Expression { get; internal init; }
    }

    public partial class Binder
    {
        private BoundExpressionStatement BindExpressionStatement(ExpressionStatement expressionStatement)
            => BoundNodeFactory.CreateBoundExpressionStatement(
                syntaxNode: expressionStatement,
                expression: BindExpression(expressionStatement.Expression));
    }
}
