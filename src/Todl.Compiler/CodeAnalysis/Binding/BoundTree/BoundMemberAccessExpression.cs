using System;
using System.Linq;
using System.Reflection;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

internal abstract class BoundMemberAccessExpression : BoundExpression
{
    public abstract BoundExpression BoundBaseExpression { get; internal init; }
    public abstract string MemberName { get; }
    public abstract bool IsStatic { get; }
    public abstract bool IsPublic { get; }

    public override bool LValue => true;
}

[BoundNode]
internal sealed class BoundClrFieldAccessExpression : BoundMemberAccessExpression
{
    public override BoundExpression BoundBaseExpression { get; internal init; }
    public FieldInfo FieldInfo { get; internal init; }
    public override string MemberName => FieldInfo.Name;
    public override bool IsStatic => FieldInfo.IsStatic;

    public override bool Constant => FieldInfo.IsLiteral;
    public override bool ReadOnly => Constant || FieldInfo.IsInitOnly;
    public override bool IsPublic => FieldInfo.IsPublic;

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundClrFieldAccessExpression(this);
}

[BoundNode]
internal sealed class BoundClrPropertyAccessExpression : BoundMemberAccessExpression
{
    public override BoundExpression BoundBaseExpression { get; internal init; }
    public PropertyInfo PropertyInfo { get; internal init; }
    public override string MemberName => PropertyInfo.Name;
    public override bool IsStatic => PropertyInfo.GetAccessors().Any(a => a.IsStatic);

    public override bool ReadOnly => PropertyInfo.GetSetMethod() is null;
    public override bool IsPublic => PropertyInfo.GetAccessors().Any(a => a.IsPublic);

    public MethodInfo GetMethod => PropertyInfo.GetMethod;
    public MethodInfo SetMethod => PropertyInfo.SetMethod;

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundClrPropertyAccessExpression(this);
}

// This is not emittable, just to place a node in the bound tree to indicate this is an error
[BoundNode]
internal sealed class BoundInvalidMemberAccessExpression : BoundMemberAccessExpression
{
    public override BoundExpression BoundBaseExpression { get; internal init; }

    public override string MemberName => string.Empty;
    public override bool IsStatic => false;
    public override bool IsPublic => true;

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundInvalidMemberAccessExpression(this);
}

public partial class Binder
{
    private BoundExpression BindMemberAccessExpression(
        MemberAccessExpression memberAccessExpression)
    {
        var boundBaseExpression = BindExpression(memberAccessExpression.BaseExpression);

        if (boundBaseExpression.ResultType is not ClrTypeSymbol clrTypeSymbol)
        {
            throw new NotSupportedException($"{boundBaseExpression.ResultType.GetType()} is not supported");
        }

        var memberInfo = clrTypeSymbol.ResolveMember(memberAccessExpression.MemberIdentifierToken.Text);

        switch (memberInfo)
        {
            case FieldInfo fieldInfo:
                if (!fieldInfo.IsStatic && boundBaseExpression is BoundTypeExpression)
                {
                    return ReportObjectReferenceRequired(memberAccessExpression, boundBaseExpression, fieldInfo.Name);
                }

                var boundFieldAccessExpression = BoundNodeFactory.CreateBoundClrFieldAccessExpression(
                    syntaxNode: memberAccessExpression,
                    boundBaseExpression: boundBaseExpression,
                    fieldInfo: fieldInfo,
                    resultType: ClrTypeCache.Resolve(fieldInfo.FieldType));

                if (!boundFieldAccessExpression.IsPublic)
                {
                    ReportNonPublicMemberAccess(boundFieldAccessExpression);
                }

                return boundFieldAccessExpression;
            case PropertyInfo propertyInfo:
                if (!propertyInfo.GetAccessors().Any(a => a.IsStatic) && boundBaseExpression is BoundTypeExpression)
                {
                    return ReportObjectReferenceRequired(memberAccessExpression, boundBaseExpression, propertyInfo.Name);
                }

                var boundPropertyAccessExpression = BoundNodeFactory.CreateBoundClrPropertyAccessExpression(
                    syntaxNode: memberAccessExpression,
                    boundBaseExpression: boundBaseExpression,
                    propertyInfo: propertyInfo,
                    resultType: ClrTypeCache.Resolve(propertyInfo.PropertyType));

                if (!boundPropertyAccessExpression.IsPublic)
                {
                    ReportNonPublicMemberAccess(boundPropertyAccessExpression);
                }

                return boundPropertyAccessExpression;
            case Type nestedType when boundBaseExpression is BoundTypeExpression:
                if (!nestedType.IsVisible)
                {
                    ReportDiagnostic(
                        new Diagnostic()
                        {
                            Message = $"Member {memberAccessExpression.MemberIdentifierToken.Text} is not public.",
                            Level = DiagnosticLevel.Error,
                            TextLocation = memberAccessExpression.GetTextLocation(),
                            ErrorCode = ErrorCode.MemberNotAccessible
                        });

                    return BoundNodeFactory.CreateBoundTypeExpression(
                        syntaxNode: memberAccessExpression,
                        resultType: null);
                }

                return BoundNodeFactory.CreateBoundTypeExpression(
                    syntaxNode: memberAccessExpression,
                    resultType: ClrTypeCache.Resolve(nestedType));
        }

        ReportDiagnostic(
            new Diagnostic()
            {
                Message = $"Member '{memberAccessExpression.MemberIdentifierToken.Text}' does not exist in type '{clrTypeSymbol.ClrType.FullName}'",
                Level = DiagnosticLevel.Error,
                TextLocation = memberAccessExpression.GetTextLocation(memberAccessExpression.MemberIdentifierToken.Span),
                ErrorCode = ErrorCode.MemberNotFound
            });

        return BoundNodeFactory.CreateBoundInvalidMemberAccessExpression(
            syntaxNode: memberAccessExpression,
            boundBaseExpression: boundBaseExpression);
    }

    private BoundMemberAccessExpression ReportObjectReferenceRequired(
        MemberAccessExpression memberAccessExpression,
        BoundExpression boundBaseExpression,
        string memberName)
    {
        ReportDiagnostic(
            new Diagnostic()
            {
                Message = $"An object reference is required to access non-static member '{memberName}'.",
                Level = DiagnosticLevel.Error,
                TextLocation = memberAccessExpression.GetTextLocation(memberAccessExpression.MemberIdentifierToken.Span),
                ErrorCode = ErrorCode.ObjectReferenceRequired
            });

        return BoundNodeFactory.CreateBoundInvalidMemberAccessExpression(
            syntaxNode: memberAccessExpression,
            boundBaseExpression: boundBaseExpression);
    }

    private void ReportNonPublicMemberAccess(BoundMemberAccessExpression boundMemberAccessExpression)
    {
        ReportDiagnostic(
            new Diagnostic()
            {
                Message = $"Member {boundMemberAccessExpression.MemberName} is not public.",
                Level = DiagnosticLevel.Error,
                TextLocation = boundMemberAccessExpression.SyntaxNode.GetTextLocation(),
                ErrorCode = ErrorCode.MemberNotAccessible
            });
    }
}
