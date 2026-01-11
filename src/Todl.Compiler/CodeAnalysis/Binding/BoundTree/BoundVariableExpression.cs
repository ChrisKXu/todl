using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundVariableExpression : BoundExpression
{
    public VariableSymbol Variable { get; internal init; }
    public override TypeSymbol ResultType => Variable.Type;
    public override bool LValue => true;
    public override bool Constant => Variable.Constant;
    public override bool ReadOnly => Variable.ReadOnly;

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundVariableExpression(this);
}

public partial class Binder
{
    /// <summary>
    /// Binds a simple name - could be a type or a variable.
    /// </summary>
    private BoundExpression BindSimpleNameExpression(SimpleNameExpression simpleNameExpression)
    {
        var name = simpleNameExpression.CanonicalName;

        // First check if it's a type (imported or built-in)
        var type = simpleNameExpression.SyntaxTree.ClrTypeCacheView.ResolveType(simpleNameExpression);
        if (type != null)
        {
            return BoundNodeFactory.CreateBoundTypeExpression(
                syntaxNode: simpleNameExpression,
                targetType: type);
        }

        // Then check if it's a variable
        var variable = Scope.LookupVariable(name);
        if (variable == null)
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = $"Undeclared variable {simpleNameExpression.Text}",
                    Level = DiagnosticLevel.Error,
                    TextLocation = simpleNameExpression.IdentifierToken.GetTextLocation(),
                    ErrorCode = ErrorCode.UndeclaredVariable
                });
        }

        return BoundNodeFactory.CreateBoundVariableExpression(
            syntaxNode: simpleNameExpression,
            variable: variable);
    }

    /// <summary>
    /// Binds a namespace-qualified expression - ALWAYS resolves to a type.
    /// This is a key semantic distinction: :: means namespace qualification,
    /// and namespace-qualified names are always types, never variables.
    /// </summary>
    private BoundExpression BindNamespaceQualifiedNameExpression(NamespaceQualifiedNameExpression NamespaceQualifiedNameExpression)
    {
        var type = NamespaceQualifiedNameExpression.SyntaxTree.ClrTypeCacheView.ResolveType(NamespaceQualifiedNameExpression);

        if (type == null)
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = $"Type '{NamespaceQualifiedNameExpression.CanonicalName}' could not be found",
                    Level = DiagnosticLevel.Error,
                    TextLocation = NamespaceQualifiedNameExpression.TypeIdentifierToken.GetTextLocation(),
                    ErrorCode = ErrorCode.TypeNotFound
                });
        }

        return BoundNodeFactory.CreateBoundTypeExpression(
            syntaxNode: NamespaceQualifiedNameExpression,
            targetType: type);
    }
}
