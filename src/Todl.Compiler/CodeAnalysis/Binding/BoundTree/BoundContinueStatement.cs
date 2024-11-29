using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundContinueStatement : BoundStatement
{
    public BoundLoopContext BoundLoopContext { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundContinueStatement(this);
}

public partial class Binder
{
    private BoundContinueStatement BindContinueStatement(ContinueStatement continueStatement)
    {
        if (BoundLoopContext is null)
        {
            ReportDiagnostic(new Diagnostic()
            {
                Level = DiagnosticLevel.Error,
                ErrorCode = ErrorCode.NoEnclosingLoop,
                Message = "No enclosing loop out of which to break or continue.",
                TextLocation = continueStatement.Text.GetTextLocation()
            });
        }

        return BoundNodeFactory.CreateBoundContinueStatement(continueStatement, BoundLoopContext);
    }
}
