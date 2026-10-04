using System;
using System.Linq;
using System.Reflection;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

internal abstract class BoundElementAccessExpression : BoundExpression
{
    public abstract BoundExpression BoundBaseExpression { get; internal init; }
    public abstract BoundExpression BoundIndexExpression { get; internal init; }

    public override bool LValue => true;
}

[BoundNode]
internal sealed class BoundArrayElementAccessExpression : BoundElementAccessExpression
{
    public override BoundExpression BoundBaseExpression { get; internal init; }
    public override BoundExpression BoundIndexExpression { get; internal init; }

    public override bool ReadOnly => false;

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundArrayElementAccessExpression(this);
}

[BoundNode]
internal sealed class BoundIndexerAccessExpression : BoundElementAccessExpression
{
    public override BoundExpression BoundBaseExpression { get; internal init; }
    public override BoundExpression BoundIndexExpression { get; internal init; }
    public PropertyInfo PropertyInfo { get; internal init; }

    public override bool ReadOnly => PropertyInfo.GetSetMethod() is null;
    public bool IsPublic => PropertyInfo.GetAccessors().Any(a => a.IsPublic);

    public MethodInfo GetMethod => PropertyInfo.GetMethod;
    public MethodInfo SetMethod => PropertyInfo.SetMethod;

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundIndexerAccessExpression(this);
}

// This is not emittable, just to place a node in the bound tree to indicate this is an error
[BoundNode]
internal sealed class BoundInvalidElementAccessExpression : BoundElementAccessExpression
{
    public override BoundExpression BoundBaseExpression { get; internal init; }
    public override BoundExpression BoundIndexExpression { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundInvalidElementAccessExpression(this);
}

public partial class Binder
{
    private BoundExpression BindElementAccessExpression(ElementAccessExpression elementAccessExpression)
    {
        var boundBaseExpression = BindExpression(elementAccessExpression.BaseExpression);
        var boundIndexExpression = BindExpression(elementAccessExpression.IndexExpression);

        if (boundBaseExpression.ResultType is not ClrTypeSymbol clrTypeSymbol)
        {
            throw new NotSupportedException($"{boundBaseExpression.ResultType?.GetType()} is not supported");
        }

        if (clrTypeSymbol.ClrType.IsArray)
        {
            if (boundIndexExpression.ResultType is null
                || boundIndexExpression.ResultType.SpecialType != SpecialType.ClrInt32)
            {
                ReportDiagnostic(
                    new Diagnostic()
                    {
                        Message = "Array index must be of type int.",
                        Level = DiagnosticLevel.Error,
                        TextLocation = elementAccessExpression.IndexExpression.GetTextLocation(),
                        ErrorCode = ErrorCode.TypeMismatch
                    });

                return BoundNodeFactory.CreateBoundInvalidElementAccessExpression(
                    syntaxNode: elementAccessExpression,
                    boundBaseExpression: boundBaseExpression,
                    boundIndexExpression: boundIndexExpression);
            }

            return BoundNodeFactory.CreateBoundArrayElementAccessExpression(
                syntaxNode: elementAccessExpression,
                boundBaseExpression: boundBaseExpression,
                boundIndexExpression: boundIndexExpression,
                resultType: ClrTypeCache.Resolve(clrTypeSymbol.ClrType.GetElementType()));
        }

        if (boundBaseExpression is BoundTypeExpression)
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = $"An object reference is required to use the indexer of '{clrTypeSymbol.Name}'.",
                    Level = DiagnosticLevel.Error,
                    TextLocation = elementAccessExpression.GetTextLocation(),
                    ErrorCode = ErrorCode.ObjectReferenceRequired
                });

            return BoundNodeFactory.CreateBoundInvalidElementAccessExpression(
                syntaxNode: elementAccessExpression,
                boundBaseExpression: boundBaseExpression,
                boundIndexExpression: boundIndexExpression);
        }

        var indexerProperty = clrTypeSymbol.ResolveIndexerCandidates()
            .FirstOrDefault(p => boundIndexExpression.ResultType is not null
                && ClrTypeCache.Resolve(p.GetIndexParameters()[0].ParameterType).Equals(boundIndexExpression.ResultType));

        if (indexerProperty is null)
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = $"Type '{clrTypeSymbol.Name}' has no indexer accepting an index of type '{boundIndexExpression.ResultType}'.",
                    Level = DiagnosticLevel.Error,
                    TextLocation = elementAccessExpression.GetTextLocation(),
                    ErrorCode = ErrorCode.NoMatchingIndexer
                });

            return BoundNodeFactory.CreateBoundInvalidElementAccessExpression(
                syntaxNode: elementAccessExpression,
                boundBaseExpression: boundBaseExpression,
                boundIndexExpression: boundIndexExpression);
        }

        var boundIndexerAccessExpression = BoundNodeFactory.CreateBoundIndexerAccessExpression(
            syntaxNode: elementAccessExpression,
            boundBaseExpression: boundBaseExpression,
            boundIndexExpression: boundIndexExpression,
            propertyInfo: indexerProperty,
            resultType: ClrTypeCache.Resolve(indexerProperty.PropertyType));

        if (!boundIndexerAccessExpression.IsPublic)
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = $"Indexer of type '{clrTypeSymbol.Name}' is not public.",
                    Level = DiagnosticLevel.Error,
                    TextLocation = elementAccessExpression.GetTextLocation(),
                    ErrorCode = ErrorCode.MemberNotAccessible
                });
        }

        return boundIndexerAccessExpression;
    }
}
