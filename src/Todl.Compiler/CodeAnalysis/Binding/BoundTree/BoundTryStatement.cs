using System.Collections.Immutable;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundTryStatement : BoundStatement
{
    public BoundBlockStatement Body { get; internal init; }
    public ImmutableArray<BoundExceptClause> ExceptClauses { get; internal init; }

    /// <summary>The finally block; null when there is no finally clause.</summary>
    public BoundBlockStatement FinallyBlock { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundTryStatement(this);
}

public partial class Binder
{
    private BoundTryStatement BindTryStatement(TryStatement tryStatement)
        => BindHandlerClauses(
            tryStatement,
            BindBlockStatement(tryStatement.BlockStatement),
            tryStatement.ExceptClauses,
            tryStatement.FinallyClause);

    /// <summary>
    /// Binds the handler clauses that follow an already bound try block, from this binder's scope.
    /// Shared by try statements and function-level handler clauses.
    /// </summary>
    private BoundTryStatement BindHandlerClauses(
        SyntaxNode syntaxNode,
        BoundBlockStatement body,
        ImmutableArray<ExceptClause> exceptClauses,
        FinallyClause finallyClause)
    {
        var boundExceptClauses = ImmutableArray.CreateBuilder<BoundExceptClause>(exceptClauses.Length);

        for (var i = 0; i < exceptClauses.Length; i++)
        {
            boundExceptClauses.Add(BindExceptClause(exceptClauses[i], boundExceptClauses, isLast: i == exceptClauses.Length - 1));
        }

        BoundBlockStatement finallyBlock = null;
        if (finallyClause is not null)
        {
            finallyBlock = CreateExceptionContextBinder(ExceptionContext.EnterFinallyBlock(BoundLoopContext))
                .BindBlockStatementInScope(finallyClause.BlockStatement);
        }

        return BoundNodeFactory.CreateBoundTryStatement(
            syntaxNode: syntaxNode,
            body: body,
            exceptClauses: boundExceptClauses.ToImmutable(),
            finallyBlock: finallyBlock);
    }
}
