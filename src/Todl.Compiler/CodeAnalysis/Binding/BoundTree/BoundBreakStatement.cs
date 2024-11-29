using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundBreakStatement : BoundStatement
{
    public BoundLoopContext BoundLoopContext { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundBreakStatement(this);
}

public partial class Binder
{
    private BoundBreakStatement BindBreakStatement(BreakStatement breakStatement)
    {
        if (BoundLoopContext is null)
        {
            ReportDiagnostic(new Diagnostic()
            {
                Level = DiagnosticLevel.Error,
                ErrorCode = ErrorCode.NoEnclosingLoop,
                Message = "No enclosing loop out of which to break or continue.",
                TextLocation = breakStatement.Text.GetTextLocation()
            });
        }

        return BoundNodeFactory.CreateBoundBreakStatement(breakStatement, BoundLoopContext);
    }
}
