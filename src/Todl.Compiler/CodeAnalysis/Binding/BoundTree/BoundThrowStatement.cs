using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundThrowStatement : BoundStatement
{
    /// <summary>The thrown expression; null for a rethrow.</summary>
    public BoundExpression Expression { get; internal init; }

    public bool IsRethrow => Expression is null;

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundThrowStatement(this);
}

public partial class Binder
{
    private BoundThrowStatement BindThrowStatement(ThrowStatement throwStatement)
    {
        BoundExpression expression = null;

        if (throwStatement.IsRethrow)
        {
            if (!ExceptionContext.InExceptBlock)
            {
                ReportDiagnostic(new Diagnostic()
                {
                    Message = "A rethrow is only valid within an except block, and not within a finally block nested in it.",
                    ErrorCode = ErrorCode.RethrowOutsideExceptBlock,
                    TextLocation = throwStatement.GetTextLocation(),
                    Level = DiagnosticLevel.Error
                });
            }
        }
        else
        {
            expression = BindExpression(throwStatement.Expression);

            if (expression is BoundTypeExpression)
            {
                ReportNotAnExceptionType("The operand of a throw statement must be a value.", throwStatement.Expression);
            }
            else if (expression.ResultType is not null && !expression.ResultType.IsExceptionType(ClrTypeCache.BuiltInTypes))
            {
                ReportNotAnExceptionType(
                    $"The operand of a throw statement must be of an exception type but is of type {expression.ResultType}.",
                    throwStatement.Expression);
            }
        }

        return BoundNodeFactory.CreateBoundThrowStatement(
            syntaxNode: throwStatement,
            expression: expression);
    }

    private void ReportNotAnExceptionType(string message, SyntaxNode syntaxNode)
        => ReportDiagnostic(new Diagnostic()
        {
            Message = message,
            ErrorCode = ErrorCode.NotAnExceptionType,
            TextLocation = syntaxNode.GetTextLocation(),
            Level = DiagnosticLevel.Error
        });
}
