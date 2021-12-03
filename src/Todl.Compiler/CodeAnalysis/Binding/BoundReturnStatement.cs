using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundReturnStatement : BoundStatement
{
    public BoundExpression BoundReturnValueExpression { get; internal init; }
}

public sealed partial class Binder
{
    private BoundReturnStatement BindReturnStatement(BoundScope scope, ReturnStatement returnStatement)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();

        BoundExpression boundReturnValueExpression = null;
        if (returnStatement.ReturnValueExpression != null)
        {
            boundReturnValueExpression = BindExpression(scope, returnStatement.ReturnValueExpression);
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
