using System.Diagnostics;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundReturnStatement : BoundStatement
{
    public BoundExpression BoundReturnValueExpression { get; internal init; }

    public TypeSymbol ReturnType => BoundReturnValueExpression?.ResultType ?? TypeSymbol.ClrVoid;
}

public partial class Binder
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

        var boundReturnStatement = new BoundReturnStatement()
        {
            SyntaxNode = returnStatement,
            BoundReturnValueExpression = boundReturnValueExpression,
            DiagnosticBuilder = diagnosticBuilder
        };

        if (FunctionSymbol is null)
        {
            diagnosticBuilder.Add(new Diagnostic()
            {
                Message = "Return statements are only valid within a function declaration.",
                ErrorCode = ErrorCode.UnexpectedStatement,
                TextLocation = returnStatement.Text.GetTextLocation(),
                Level = DiagnosticLevel.Error
            });
        }
        else if (boundReturnStatement.ReturnType != FunctionSymbol.ReturnType)
        {
            diagnosticBuilder.Add(new Diagnostic()
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
