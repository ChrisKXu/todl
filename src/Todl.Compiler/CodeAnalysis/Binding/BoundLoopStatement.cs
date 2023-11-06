using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundLoopStatement : BoundStatement
{
    public BoundExpression Condition { get; internal init; }
    public bool ConditionNegated { get; internal init; }
    public BoundStatement Body { get; internal init; }
}

public partial class Binder
{
    private BoundLoopStatement BindWhileUntilStatement(WhileUntilStatement whileUntilStatement)
    {
        var condition = BindExpression(whileUntilStatement.ConditionExpression);
        var body = BindBlockStatement(whileUntilStatement.BlockStatement);
        var negated = whileUntilStatement.WhileOrUntilToken.Kind == SyntaxKind.UntilKeywordToken;
        var diagnosticBuilder = new DiagnosticBag.Builder();

        if (condition.ResultType.SpecialType != Symbols.SpecialType.ClrBoolean)
        {
            diagnosticBuilder.Add(new Diagnostic()
            {
                ErrorCode = ErrorCode.TypeMismatch,
                Level = DiagnosticLevel.Error,
                TextLocation = whileUntilStatement.ConditionExpression.Text.GetTextLocation(),
                Message = "Condition must be of boolean type."
            });
        }

        return BoundNodeFactory.CreateBoundLoopStatement(
            whileUntilStatement,
            condition,
            negated,
            body,
            diagnosticBuilder);
    }
}
