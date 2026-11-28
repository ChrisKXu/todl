using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundVariableDeclarationStatement : BoundStatement
{
    public LocalVariableSymbol Variable { get; internal init; }
    public BoundExpression InitializerExpression { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundVariableDeclarationStatement(this);
}

public partial class Binder
{
    private BoundVariableDeclarationStatement BindVariableDeclarationStatement(
        VariableDeclarationStatement variableDeclarationStatement)
    {
        var declaredType = BindDeclaredVariableType(variableDeclarationStatement.TypeExpression);
        var initializerExpression = variableDeclarationStatement.TypeExpression is null
            ? BindExpression(variableDeclarationStatement.InitializerExpression)
            : declaredType is null
                ? BindExpressionDeferringNullLiteral(variableDeclarationStatement.InitializerExpression)
                : BindExpressionWithTargetType(variableDeclarationStatement.InitializerExpression, declaredType);

        if (declaredType is not null
            && initializerExpression.ResultType is not null
            && !initializerExpression.ResultType.Equals(declaredType))
        {
            ReportDiagnostic(new Diagnostic()
            {
                Message = $"Cannot initialize variable '{variableDeclarationStatement.IdentifierToken.Text}' of type {declaredType} with a value of type {initializerExpression.ResultType}.",
                Level = DiagnosticLevel.Error,
                TextLocation = variableDeclarationStatement.InitializerExpression.GetTextLocation(),
                ErrorCode = ErrorCode.TypeMismatch
            });
        }

        var variable = new LocalVariableSymbol()
        {
            VariableDeclarationStatement = variableDeclarationStatement,
            BoundInitializer = initializerExpression,
            DeclaredType = declaredType
        };

        DeclareLocalVariable(
            variable,
            variableDeclarationStatement.GetTextLocation(variableDeclarationStatement.IdentifierToken.Span));

        return BoundNodeFactory.CreateBoundVariableDeclarationStatement(
            syntaxNode: variableDeclarationStatement,
            variable: variable,
            initializerExpression: initializerExpression);
    }

    private TypeSymbol BindDeclaredVariableType(TypeExpression typeExpression)
    {
        if (typeExpression is null || !ValidateNullableTypeExpression(typeExpression))
        {
            return null;
        }

        var type = GetClrTypeCacheView(typeExpression.SyntaxTree).ResolveType(typeExpression);
        if (type is null)
        {
            ReportDiagnostic(new Diagnostic()
            {
                Message = $"Type {typeExpression.GetText()} is invalid",
                Level = DiagnosticLevel.Error,
                TextLocation = typeExpression.GetTextLocation(),
                ErrorCode = ErrorCode.TypeNotFound
            });

            return null;
        }

        if (type.SpecialType == SpecialType.ClrVoid)
        {
            ReportDiagnostic(new Diagnostic()
            {
                Message = "A variable cannot be of type void.",
                Level = DiagnosticLevel.Error,
                TextLocation = typeExpression.GetTextLocation(),
                ErrorCode = ErrorCode.InvalidVariableType
            });

            return null;
        }

        return type;
    }
}
