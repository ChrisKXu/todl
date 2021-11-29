using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundReturnStatement : BoundStatement
{
    public BoundExpression BoundReturnValueExpression { get; internal init; }
}

public sealed partial class Binder
{
    private BoundReturnStatement BindReturnStatement(BoundScope scope, ReturnStatement returnStatement)
    {
        BoundExpression boundReturnValueExpression = null;

        if (returnStatement.ReturnValueExpression != null)
        {
            boundReturnValueExpression = BindExpression(scope, returnStatement.ReturnValueExpression);
        }

        return new()
        {
            SyntaxNode = returnStatement,
            BoundReturnValueExpression = boundReturnValueExpression
        };
    }
}
