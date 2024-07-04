using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
public sealed class BoundBreakStatement : BoundStatement
{
    public BoundLoopContext BoundLoopContext { get; internal init; }
}

public partial class Binder
{
    private BoundBreakStatement BindBreakStatement(BreakStatement breakStatement)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();

        if (BoundLoopContext is null)
        {
            diagnosticBuilder.Add(new Diagnostic()
            {
                Level = DiagnosticLevel.Error,
                ErrorCode = ErrorCode.NoEnclosingLoop,
                Message = "No enclosing loop out of which to break or continue.",
                TextLocation = breakStatement.Text.GetTextLocation()
            });
        }

        return BoundNodeFactory.CreateBoundBreakStatement(breakStatement, BoundLoopContext, diagnosticBuilder);
    }
}
