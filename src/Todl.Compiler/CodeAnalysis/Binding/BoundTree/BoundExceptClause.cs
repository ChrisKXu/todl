using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundExceptClause : BoundNode
{
    public TypeSymbol ExceptionType { get; internal init; }

    public ExceptionVariableSymbol ExceptionVariable { get; internal init; }

    public BoundBlockStatement Body { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundExceptClause(this);
}

public partial class Binder
{
    private BoundExceptClause BindExceptClause(
        ExceptClause exceptClause,
        IReadOnlyList<BoundExceptClause> earlierClauses,
        bool isLast)
    {
        // the exception variable's declaration space encloses the except block's own
        var clauseBinder = CreateExceptionContextBinder(ExceptionContext.EnterExceptBlock());
        TypeSymbol exceptionType = null;
        ExceptionVariableSymbol exceptionVariable = null;

        if (exceptClause.IsGeneralClause)
        {
            if (!isLast)
            {
                ReportDiagnostic(new Diagnostic()
                {
                    Message = "A general except clause must be the last except clause and can appear at most once.",
                    ErrorCode = ErrorCode.MisplacedGeneralExceptClause,
                    TextLocation = exceptClause.GetTextLocation(exceptClause.ExceptKeywordToken.Span),
                    Level = DiagnosticLevel.Error
                });
            }
        }
        else
        {
            exceptionType = BindExceptionType(exceptClause, earlierClauses);

            if (exceptClause.IdentifierToken.HasValue && exceptionType is not null)
            {
                exceptionVariable = new ExceptionVariableSymbol(exceptClause, exceptionType);
                clauseBinder.DeclareLocalVariable(
                    exceptionVariable,
                    exceptClause.GetTextLocation(exceptClause.IdentifierToken.Value.Span));
            }
        }

        return BoundNodeFactory.CreateBoundExceptClause(
            syntaxNode: exceptClause,
            exceptionType: exceptionType,
            exceptionVariable: exceptionVariable,
            body: clauseBinder.BindBlockStatement(exceptClause.BlockStatement));
    }

    private TypeSymbol BindExceptionType(ExceptClause exceptClause, IReadOnlyList<BoundExceptClause> earlierClauses)
    {
        var typeExpression = exceptClause.TypeExpression;
        var type = GetClrTypeCacheView(typeExpression.SyntaxTree).ResolveType(typeExpression);

        if (type is null)
        {
            ReportDiagnostic(new Diagnostic()
            {
                Message = $"Type {typeExpression.GetText()} is invalid",
                ErrorCode = ErrorCode.TypeNotFound,
                TextLocation = typeExpression.GetTextLocation(),
                Level = DiagnosticLevel.Error
            });

            return null;
        }

        if (!type.IsExceptionType)
        {
            ReportNotAnExceptionType($"Type {type} is not an exception type.", typeExpression);
            return type;
        }

        foreach (var earlier in earlierClauses)
        {
            if (earlier.ExceptionType is ClrTypeSymbol earlierType
                && (type.Equals(earlierType) || type.DerivesFrom(earlierType)))
            {
                ReportDiagnostic(new Diagnostic()
                {
                    Message = $"Except clause for {type} is unreachable because an earlier clause already handles {earlierType}.",
                    ErrorCode = ErrorCode.UnreachableExceptClause,
                    TextLocation = typeExpression.GetTextLocation(),
                    Level = DiagnosticLevel.Error
                });

                break;
            }
        }

        return type;
    }
}
