using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundBlockStatement : BoundStatement
    {
        public BoundScope Scope { get; }
        public IReadOnlyList<BoundStatement> Statements { get; }

        public BoundBlockStatement(BoundScope scope, IReadOnlyList<BoundStatement> statements)
        {
            this.Scope = scope;
            this.Statements = statements;
        }
    }

    public sealed partial class Binder
    {
        private BoundBlockStatement BindBlockStatement(BoundScope parentScope, BlockStatement blockStatement)
        {
            var scope = parentScope.CreateChildScope(BoundScopeKind.BlockStatement);
            var boundStatements = blockStatement.InnerStatements.Select(statement => BindStatement(scope, statement));
            return new BoundBlockStatement(scope, boundStatements.ToList());
        }
    }
}
