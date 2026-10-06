using System.Collections.Immutable;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundArrayLiteralExpression : BoundExpression
{
    public ImmutableArray<BoundExpression> BoundElements { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundArrayLiteralExpression(this);
}

public partial class Binder
{
    private BoundExpression BindArrayLiteralExpression(ArrayLiteralExpression arrayLiteralExpression)
    {
        var boundElements = arrayLiteralExpression.Elements
            .Select(BindExpression)
            .ToImmutableArray();

        var elementType = boundElements[0].ResultType;
        var isValid = elementType is not null;

        for (var i = 1; i < boundElements.Length; ++i)
        {
            var currentType = boundElements[i].ResultType;

            if (currentType is null)
            {
                isValid = false;
                continue;
            }

            if (elementType is not null && !currentType.Equals(elementType))
            {
                isValid = false;
                ReportDiagnostic(
                    new Diagnostic()
                    {
                        Message = $"Array element type mismatch: expected '{elementType}', got '{currentType}'.",
                        Level = DiagnosticLevel.Error,
                        TextLocation = arrayLiteralExpression.Elements[i].GetTextLocation(),
                        ErrorCode = ErrorCode.ArrayElementTypeMismatch
                    });
            }
        }

        if (!isValid)
        {
            return BoundNodeFactory.CreateBoundArrayLiteralExpression(
                syntaxNode: arrayLiteralExpression,
                boundElements: boundElements,
                resultType: null);
        }

        var arrayType = GetClrTypeCacheView(arrayLiteralExpression.SyntaxTree)
            .ResolveArrayType((ClrTypeSymbol)elementType, 1);

        return BoundNodeFactory.CreateBoundArrayLiteralExpression(
            syntaxNode: arrayLiteralExpression,
            boundElements: boundElements,
            resultType: arrayType);
    }
}
