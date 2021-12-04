using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundReturnStatement : BoundStatement
{
    public BoundExpression BoundReturnValueExpression { get; internal init; }
}

public sealed partial class Binder
{
    private BoundReturnStatement BindReturnStatement(ReturnStatement returnStatement)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();

        BoundExpression boundReturnValueExpression = null;
        if (returnStatement.ReturnValueExpression != null)
        {
            boundReturnValueExpression = BindExpression(returnStatement.ReturnValueExpression);
            diagnosticBuilder.Add(boundReturnValueExpression);
        }

        return new()
        {
            SyntaxNode = returnStatement,
            BoundReturnValueExpression = boundReturnValueExpression,
            DiagnosticBuilder = diagnosticBuilder
        };
    }
}
