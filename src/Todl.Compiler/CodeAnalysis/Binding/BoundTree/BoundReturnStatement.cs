using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
public sealed class BoundReturnStatement : BoundStatement
{
    public BoundExpression BoundReturnValueExpression { get; internal init; }

    public TypeSymbol ReturnType
        => BoundReturnValueExpression?.ResultType
        ?? SyntaxNode.SyntaxTree.ClrTypeCache.BuiltInTypes.Void;
}

public partial class Binder
{
    private BoundReturnStatement BindReturnStatement(ReturnStatement returnStatement)
    {
        BoundExpression boundReturnValueExpression = null;
        if (returnStatement.ReturnValueExpression != null)
        {
            boundReturnValueExpression = BindExpression(returnStatement.ReturnValueExpression);
        }

        var boundReturnStatement = BoundNodeFactory.CreateBoundReturnStatement(
            syntaxNode: returnStatement,
            boundReturnValueExpression: boundReturnValueExpression);

        if (!IsInFunction)
        {
            boundReturnStatement.DiagnosticBuilder.Add(new Diagnostic()
            {
                Message = "Return statements are only valid within a function declaration.",
                ErrorCode = ErrorCode.UnexpectedStatement,
                TextLocation = returnStatement.Text.GetTextLocation(),
                Level = DiagnosticLevel.Error
            });
        }
        else if (!boundReturnStatement.ReturnType.Equals(FunctionSymbol.ReturnType))
        {
            boundReturnStatement.DiagnosticBuilder.Add(new Diagnostic()
            {
                Message = $"The function expects a return type of {FunctionSymbol.ReturnType} but {boundReturnStatement.ReturnType} is returned.",
                ErrorCode = ErrorCode.TypeMismatch,
                TextLocation = returnStatement.Text.GetTextLocation(),
                Level = DiagnosticLevel.Error
            });
        }

        return boundReturnStatement;
    }
}
