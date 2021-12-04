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

    public partial class Binder
    {
        private BoundBlockStatement BindBlockStatementInScope(BlockStatement blockStatement)
            => new()
            {
                SyntaxNode = blockStatement,
                Scope = Scope,
                Statements = blockStatement.InnerStatements.Select(statement => BindStatement(statement)).ToList()
            };

        private BoundBlockStatement BindBlockStatement(BlockStatement blockStatement)
            => CreateBlockStatementBinder().BindBlockStatementInScope(blockStatement);
    }
}
