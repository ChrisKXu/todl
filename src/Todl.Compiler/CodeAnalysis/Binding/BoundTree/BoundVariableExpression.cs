﻿using Todl.Compiler.CodeAnalysis.Symbols;
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
    private BoundExpression BindNameExpression(NameExpression nameExpression)
    {
        var name = nameExpression.Text.ToString();
        var type = nameExpression.SyntaxTree.ClrTypeCacheView.ResolveType(nameExpression);

        if (type != null)
        {
            return BoundNodeFactory.CreateBoundTypeExpression(
                syntaxNode: nameExpression,
                targetType: type);
        }

        var variable = Scope.LookupVariable(name);
        if (variable == null)
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = $"Undeclared variable {nameExpression.Text}",
                    Level = DiagnosticLevel.Error,
                    TextLocation = nameExpression.SyntaxTokens[0].GetTextLocation(),
                    ErrorCode = ErrorCode.UndeclaredVariable
                });
        }

        return BoundNodeFactory.CreateBoundVariableExpression(
            syntaxNode: nameExpression,
            variable: variable);
    }
}
