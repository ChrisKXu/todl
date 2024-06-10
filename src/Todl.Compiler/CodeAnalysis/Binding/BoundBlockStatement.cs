using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    [BoundNode]
    public sealed class BoundBlockStatement : BoundStatement
    {
        public BoundScope Scope { get; internal init; }
        public IReadOnlyList<BoundStatement> Statements { get; internal init; }
    }

    public partial class Binder
    {
        private BoundBlockStatement BindBlockStatementInScope(BlockStatement blockStatement)
            => BoundNodeFactory.CreateBoundBlockStatement(
                syntaxNode: blockStatement,
                scope: Scope,
                statements: blockStatement.InnerStatements.Select(statement => BindStatement(statement)).ToList());

        private BoundBlockStatement BindBlockStatement(BlockStatement blockStatement)
            => CreateBlockStatementBinder().BindBlockStatementInScope(blockStatement);
    }
}
