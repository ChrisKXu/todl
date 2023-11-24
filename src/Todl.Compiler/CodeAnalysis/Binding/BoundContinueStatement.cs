using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundContinueStatement : BoundStatement
{
}

public partial class Binder
{
    protected virtual BoundContinueStatement BindContinueStatement(ContinueStatement continueStatement)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        diagnosticBuilder.Add(new Diagnostic()
        {
            Level = DiagnosticLevel.Error,
            ErrorCode = ErrorCode.NoEnclosingLoop,
            Message = "No enclosing loop out of which to break or continue.",
            TextLocation = continueStatement.Text.GetTextLocation()
        });

        return BoundNodeFactory.CreateBoundContinueStatement(continueStatement, diagnosticBuilder);
    }

    internal partial class LoopBinder
    {
        protected override BoundContinueStatement BindContinueStatement(ContinueStatement continueStatement)
            => BoundNodeFactory.CreateBoundContinueStatement(continueStatement);
    }
}
