using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
public sealed class BoundContinueStatement : BoundStatement
{
    public BoundLoopContext BoundLoopContext { get; internal init; }
}

public partial class Binder
{
    private BoundContinueStatement BindContinueStatement(ContinueStatement continueStatement)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();

        if (BoundLoopContext is null)
        {
            diagnosticBuilder.Add(new Diagnostic()
            {
                Level = DiagnosticLevel.Error,
                ErrorCode = ErrorCode.NoEnclosingLoop,
                Message = "No enclosing loop out of which to break or continue.",
                TextLocation = continueStatement.Text.GetTextLocation()
            });
        }

        return BoundNodeFactory.CreateBoundContinueStatement(continueStatement, BoundLoopContext, diagnosticBuilder);
    }
}
