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
        private BoundBlockStatement BindBlockStatement(BoundScope scope, BlockStatement blockStatement)
            => new BoundBlockStatement()
            {
                Scope = scope,
                Statements = blockStatement.InnerStatements.Select(statement => BindStatement(scope, statement)).ToList()
            };
    }
}
