using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundExpressionStatement : BoundStatement
    {
        public BoundExpression Expression { get; internal init; }

        public override IEnumerable<Diagnostic> GetDiagnostics()
            => Expression.GetDiagnostics();
    }

    public partial class Binder
    {
        private BoundExpressionStatement BindExpressionStatement(ExpressionStatement expressionStatement)
            => new()
            {
                SyntaxNode = expressionStatement,
                Expression = BindExpression(expressionStatement.Expression)
            };
    }
}
