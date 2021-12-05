using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundBlockStatement : BoundStatement
    {
        public BoundScope Scope { get; internal init; }
        public IReadOnlyList<BoundStatement> Statements { get; internal init; }

        public override IEnumerable<Diagnostic> GetDiagnostics()
        {
            var builder = new DiagnosticBag.Builder();
            builder.AddRange(Statements);

            return builder.Build();
        }
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
