using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundTypeExpression : BoundExpression
{
    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundTypeExpression(this);
}

public partial class Binder
{
    private BoundTypeExpression BindTypeExpression(NameExpression nameExpression)
    {
        var type = GetClrTypeCacheView(nameExpression.SyntaxTree).ResolveType(nameExpression);

        if (type is null)
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = $"Type {nameExpression.GetText()} is invalid",
                    Level = DiagnosticLevel.Error,
                    TextLocation = nameExpression.GetTextLocation(),
                    ErrorCode = ErrorCode.TypeNotFound
                });
        }

        return BoundNodeFactory.CreateBoundTypeExpression(
            syntaxNode: nameExpression,
            resultType: type);
    }

    // Reports '?' applied to a type that is not a non-nullable value type; an unresolvable base type is reported elsewhere.
    private bool ValidateNullableTypeExpression(TypeExpression typeExpression)
    {
        if (!typeExpression.IsNullableType)
        {
            return true;
        }

        var baseType = GetClrTypeCacheView(typeExpression.SyntaxTree).ResolveType(typeExpression.BaseTypeExpression);

        if (baseType is null || ClrTypeCache.ResolveNullable(baseType) is not null)
        {
            return true;
        }

        ReportInvalidNullableBase(baseType, typeExpression.GetTextLocation());
        return false;
    }

    private void ReportInvalidNullableBase(TypeSymbol baseType, TextLocation textLocation)
    {
        var reason = baseType.SpecialType == SpecialType.ClrVoid
            ? "'void' cannot be made nullable."
            : baseType.IsNullableValueType
                ? $"'{baseType}' is already a nullable type."
                : $"'{baseType}' is a reference type; only value types can be made nullable.";

        ReportDiagnostic(new Diagnostic()
        {
            Message = reason,
            Level = DiagnosticLevel.Error,
            TextLocation = textLocation,
            ErrorCode = ErrorCode.InvalidNullableType
        });
    }
}
