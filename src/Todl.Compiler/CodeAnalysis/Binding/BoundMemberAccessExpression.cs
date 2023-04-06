using System;
using System.Linq;
using System.Reflection;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

public abstract class BoundMemberAccessExpression : BoundExpression
{
    public abstract BoundExpression BoundBaseExpression { get; internal init; }
    public abstract string MemberName { get; }
    public abstract bool IsStatic { get; }
    public abstract bool IsPublic { get; }
}

public sealed class BoundClrFieldAccessExpression : BoundMemberAccessExpression
{
    public override BoundExpression BoundBaseExpression { get; internal init; }
    public FieldInfo FieldInfo { get; internal init; }
    public override string MemberName => FieldInfo.Name;
    public override bool IsStatic => FieldInfo.IsStatic;

    public override TypeSymbol ResultType
        => SyntaxNode.SyntaxTree.ClrTypeCache.Resolve(FieldInfo.FieldType);

    public override bool Constant => FieldInfo.IsLiteral;
    public override bool LValue => true;
    public override bool ReadOnly => Constant || FieldInfo.IsInitOnly;
    public override bool IsPublic => FieldInfo.IsPublic;
}

public sealed class BoundClrPropertyAccessExpression : BoundMemberAccessExpression
{
    public override BoundExpression BoundBaseExpression { get; internal init; }
    public PropertyInfo PropertyInfo { get; internal init; }
    public override string MemberName => PropertyInfo.Name;
    public override bool IsStatic => PropertyInfo.GetAccessors().Any(a => a.IsStatic);

    public override TypeSymbol ResultType
        => SyntaxNode.SyntaxTree.ClrTypeCache.Resolve(PropertyInfo.PropertyType);

    public override bool ReadOnly => PropertyInfo.GetSetMethod() is null;
    public override bool IsPublic => PropertyInfo.GetAccessors().Any(a => a.IsPublic);
}

// This is not emittable, just to place a node in the bound tree to indicate this is an error
public sealed class BoundInvalidMemberAccessExpression : BoundMemberAccessExpression
{
    public override BoundExpression BoundBaseExpression { get; internal init; }

    public override string MemberName => string.Empty;
    public override bool IsStatic => false;
    public override bool IsPublic => true;
}

public partial class Binder
{
    private BoundMemberAccessExpression BindMemberAccessExpression(
        MemberAccessExpression memberAccessExpression)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundBaseExpression = BindExpression(memberAccessExpression.BaseExpression);

        if (boundBaseExpression.ResultType is not ClrTypeSymbol clrTypeSymbol)
        {
            throw new NotSupportedException($"{boundBaseExpression.ResultType.GetType()} is not supported");
        }

        var memberInfo = clrTypeSymbol.ResolveMember(memberAccessExpression.MemberIdentifierToken.Text);

        switch (memberInfo)
        {
            case FieldInfo fieldInfo:
                var boundFieldAccessExpression = BoundNodeFactory.CreateBoundClrFieldAccessExpression(
                    syntaxNode: memberAccessExpression,
                    boundBaseExpression: boundBaseExpression,
                    fieldInfo: fieldInfo,
                    diagnosticBuilder: diagnosticBuilder);

                if (!boundFieldAccessExpression.IsPublic)
                {
                    ReportNonPublicMemberAccess(boundFieldAccessExpression, diagnosticBuilder);
                }

                return boundFieldAccessExpression;
            case PropertyInfo propertyInfo:
                var boundPropertyAccessExpression = BoundNodeFactory.CreateBoundClrPropertyAccessExpression(
                    syntaxNode: memberAccessExpression,
                    boundBaseExpression: boundBaseExpression,
                    propertyInfo: propertyInfo,
                    diagnosticBuilder: diagnosticBuilder);

                if (!boundPropertyAccessExpression.IsPublic)
                {
                    ReportNonPublicMemberAccess(boundPropertyAccessExpression, diagnosticBuilder);
                }

                return boundPropertyAccessExpression;
        }

        diagnosticBuilder.Add(
            new Diagnostic()
            {
                Message = $"Member {memberAccessExpression.MemberIdentifierToken.Text} does not exist in type {clrTypeSymbol.ClrType.FullName}",
                Level = DiagnosticLevel.Error,
                TextLocation = memberAccessExpression.MemberIdentifierToken.GetTextLocation(),
                ErrorCode = ErrorCode.MemberNotFound
            });

        return BoundNodeFactory.CreateBoundInvalidMemberAccessExpression(
            syntaxNode: memberAccessExpression,
            boundBaseExpression: boundBaseExpression,
            diagnosticBuilder: diagnosticBuilder);
    }

    private void ReportNonPublicMemberAccess(BoundMemberAccessExpression boundMemberAccessExpression, DiagnosticBag.Builder diagnosticBuilder)
    {
        diagnosticBuilder.Add(
            new Diagnostic()
            {
                Message = $"Member {boundMemberAccessExpression.MemberName} is not public.",
                Level = DiagnosticLevel.Error,
                TextLocation = boundMemberAccessExpression.SyntaxNode.Text.GetTextLocation(),
                ErrorCode = ErrorCode.MemberNotAccessible
            });
    }
}
