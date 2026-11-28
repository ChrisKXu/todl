using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

// The null literal has no type of its own; it is only valid once wrapped in a BoundConversionExpression to a target nullable value type.
[BoundNode]
internal sealed class BoundNullLiteralExpression : BoundExpression
{
    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundNullLiteralExpression(this);
}

public partial class Binder
{
    private static Expression UnwrapParentheses(Expression expression)
    {
        while (expression is ParethesizedExpression parethesizedExpression)
        {
            expression = parethesizedExpression.InnerExpression;
        }

        return expression;
    }

    // Binds a null literal where no target type is available.
    private BoundNullLiteralExpression BindNullLiteralExpression(NullLiteralExpression nullLiteralExpression)
    {
        ReportDiagnostic(new Diagnostic()
        {
            Message = "The null literal has no type here; it is only valid where a nullable value type (T?) is expected.",
            Level = DiagnosticLevel.Error,
            TextLocation = nullLiteralExpression.GetTextLocation(),
            ErrorCode = ErrorCode.UntypedNullLiteral
        });

        return CreateBoundNullLiteralExpression(nullLiteralExpression);
    }

    // Binds an expression whose target type is only known later; a null literal is left untyped and not reported.
    private BoundExpression BindExpressionDeferringNullLiteral(Expression expression)
        => UnwrapParentheses(expression) is NullLiteralExpression nullLiteralExpression
            ? CreateBoundNullLiteralExpression(nullLiteralExpression)
            : BindExpression(expression);

    // Binds an expression against a known target type, applying identity/T -> T? wrapping/null -> T?.
    private BoundExpression BindExpressionWithTargetType(Expression expression, TypeSymbol targetType)
    {
        var bound = BindExpressionDeferringNullLiteral(expression);

        if (bound is not BoundNullLiteralExpression)
        {
            return TryConvertImplicit(bound, targetType) ?? bound;
        }

        return ConvertNullLiteral(bound, targetType);
    }

    private BoundExpression ConvertNullLiteral(BoundExpression nullLiteral, TypeSymbol targetType)
    {
        var converted = TryConvertImplicit(nullLiteral, targetType);
        if (converted is not null)
        {
            return converted;
        }

        ReportDiagnostic(new Diagnostic()
        {
            Message = targetType is null
                ? "The null literal has no type here; it is only valid where a nullable value type (T?) is expected."
                : $"Cannot convert null to type {targetType}; null is only valid for nullable value types (T?).",
            Level = DiagnosticLevel.Error,
            TextLocation = nullLiteral.SyntaxNode.GetTextLocation(),
            ErrorCode = targetType is null ? ErrorCode.UntypedNullLiteral : ErrorCode.TypeMismatch
        });

        return nullLiteral;
    }

    // Identity returns the expression itself; returns null when there is no applicable implicit conversion.
    // Numeric widening is deliberately not applied.
    private static BoundExpression TryConvertImplicit(BoundExpression expression, TypeSymbol targetType)
    {
        var conversion = expression is BoundNullLiteralExpression
            ? Conversions.ClassifyNullLiteral(targetType)
            : Conversions.ClassifyImplicit(expression.ResultType, targetType);

        return conversion.Kind switch
        {
            ConversionKind.Identity => expression,
            ConversionKind.ImplicitNullable or ConversionKind.NullLiteral
                => BoundNodeFactory.CreateBoundConversionExpression(
                    syntaxNode: expression.SyntaxNode,
                    operand: expression,
                    conversionKind: conversion.Kind,
                    resultType: targetType),
            _ => null
        };
    }

    private static BoundNullLiteralExpression CreateBoundNullLiteralExpression(NullLiteralExpression nullLiteralExpression)
        => BoundNodeFactory.CreateBoundNullLiteralExpression(syntaxNode: nullLiteralExpression, resultType: null);
}
