using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundTypeExpression : BoundExpression
{
    internal TypeSymbol TargetType { get; init; }

    public override TypeSymbol ResultType => TargetType;
}

public partial class Binder
{
    private BoundTypeExpression BindTypeExpression(NameExpression nameExpression)
    {
        var type = nameExpression.SyntaxTree.ClrTypeCacheView.ResolveType(nameExpression);
        var diagnosticBuilder = new DiagnosticBag.Builder();

        if (type is null)
        {
            diagnosticBuilder.Add(
                new Diagnostic()
                {
                    Message = $"Type {nameExpression.Text} is invalid",
                    Level = DiagnosticLevel.Error,
                    TextLocation = nameExpression.Text.GetTextLocation(),
                    ErrorCode = ErrorCode.TypeNotFound
                });
        }

        return BoundNodeFactory.CreateBoundTypeExpression(
            syntaxNode: nameExpression,
            targetType: type);
    }
}
