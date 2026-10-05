using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundArrayCreationExpression : BoundExpression
{
    public BoundExpression BoundLengthExpression { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundArrayCreationExpression(this);
}

public partial class Binder
{
    private BoundExpression BindArrayCreationExpression(ArrayCreationExpression arrayCreationExpression)
    {
        var elementTypeExpression = BindTypeExpression(arrayCreationExpression.ElementTypeNameExpression);
        var boundLengthExpression = BindExpression(arrayCreationExpression.LengthExpression);

        if (boundLengthExpression.ResultType is not null
            && boundLengthExpression.ResultType.SpecialType != SpecialType.ClrInt32)
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = "Array length must be of type int.",
                    Level = DiagnosticLevel.Error,
                    TextLocation = arrayCreationExpression.LengthExpression.GetTextLocation(),
                    ErrorCode = ErrorCode.TypeMismatch
                });
        }

        if (elementTypeExpression.ResultType is null)
        {
            return BoundNodeFactory.CreateBoundArrayCreationExpression(
                syntaxNode: arrayCreationExpression,
                boundLengthExpression: boundLengthExpression,
                resultType: null);
        }

        if (elementTypeExpression.ResultType.SpecialType == SpecialType.ClrVoid)
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = "Array element type cannot be void.",
                    Level = DiagnosticLevel.Error,
                    TextLocation = arrayCreationExpression.ElementTypeNameExpression.GetTextLocation(),
                    ErrorCode = ErrorCode.InvalidArrayElementType
                });

            return BoundNodeFactory.CreateBoundArrayCreationExpression(
                syntaxNode: arrayCreationExpression,
                boundLengthExpression: boundLengthExpression,
                resultType: null);
        }

        var rank = 1 + arrayCreationExpression.ArrayRankSpecifiers.Length;
        var arrayType = GetClrTypeCacheView(arrayCreationExpression.SyntaxTree)
            .ResolveArrayType((ClrTypeSymbol)elementTypeExpression.ResultType, rank);

        return BoundNodeFactory.CreateBoundArrayCreationExpression(
            syntaxNode: arrayCreationExpression,
            boundLengthExpression: boundLengthExpression,
            resultType: arrayType);
    }
}
