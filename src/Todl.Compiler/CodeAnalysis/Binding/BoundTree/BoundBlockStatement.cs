using System.Collections.Immutable;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundBlockStatement : BoundStatement
{
    public BoundScope Scope { get; internal init; }
    public ImmutableArray<BoundStatement> Statements { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundBlockStatement(this);
}

public partial class Binder
{
    private BoundBlockStatement BindBlockStatementInScope(BlockStatement blockStatement)
        => BoundNodeFactory.CreateBoundBlockStatement(
            syntaxNode: blockStatement,
            scope: Scope,
            statements: ImmutableArray.CreateRange(blockStatement.InnerStatements.Select(BindStatement)));

    private BoundBlockStatement BindBlockStatement(BlockStatement blockStatement)
        => CreateBlockStatementBinder().BindBlockStatementInScope(blockStatement);
}
