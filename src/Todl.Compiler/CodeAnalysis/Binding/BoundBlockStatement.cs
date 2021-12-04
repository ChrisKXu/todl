using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundBlockStatement : BoundStatement
    {
        public BoundScope Scope { get; internal init; }
        public IReadOnlyList<BoundStatement> Statements { get; internal init; }
    }

    public sealed partial class Binder
    {
        private BoundBlockStatement BindBlockStatementInScope(BlockStatement blockStatement)
            => new()
            {
                SyntaxNode = blockStatement,
                Scope = scope,
                Statements = blockStatement.InnerStatements.Select(statement => BindStatement(statement)).ToList()
            };

        private BoundBlockStatement BindBlockStatement(BlockStatement blockStatement)
        {
            var childScope = scope.CreateChildScope(BoundScopeKind.BlockStatement);
            var childBinder = new Binder(binderFlags, childScope);
            return childBinder.BindBlockStatementInScope(blockStatement);
        }
    }
}
