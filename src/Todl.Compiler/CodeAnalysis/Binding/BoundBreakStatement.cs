using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundBreakStatement : BoundStatement
{
}

public partial class Binder
{
    protected virtual BoundBreakStatement BindBreakStatement(BreakStatement breakStatement)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        diagnosticBuilder.Add(new Diagnostic()
        {
            Level = DiagnosticLevel.Error,
            ErrorCode = ErrorCode.NoEnclosingLoop,
            Message = "No enclosing loop out of which to break or continue.",
            TextLocation = breakStatement.Text.GetTextLocation()
        });

        return BoundNodeFactory.CreateBoundBreakStatement(breakStatement, diagnosticBuilder);
    }

    internal partial class LoopBinder
    {
        protected override BoundBreakStatement BindBreakStatement(BreakStatement breakStatement)
            => BoundNodeFactory.CreateBoundBreakStatement(breakStatement);
    }
}
