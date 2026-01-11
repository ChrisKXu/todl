using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundAssignmentExpression : BoundExpression
{
    public sealed class BoundAssignmentOperator
    {
        public SyntaxKind SyntaxKind { get; }
        public BoundAssignmentOperatorKind BoundAssignmentOperatorKind { get; }

        public BoundAssignmentOperator(SyntaxKind syntaxKind, BoundAssignmentOperatorKind boundAssignmentOperatorKind)
        {
            SyntaxKind = syntaxKind;
            BoundAssignmentOperatorKind = boundAssignmentOperatorKind;
        }
    }

    public enum BoundAssignmentOperatorKind
    {
        Assignment,
        AdditionInline,
        SubstractionInline,
        MultiplicationInline,
        DivisionInline
    }

    private static readonly IReadOnlyDictionary<SyntaxKind, BoundAssignmentOperator> supportedAssignmentOperators = new Dictionary<SyntaxKind, BoundAssignmentOperator>()
    {
        { SyntaxKind.EqualsToken, new BoundAssignmentOperator(SyntaxKind.EqualsToken, BoundAssignmentOperatorKind.Assignment) },
        { SyntaxKind.PlusEqualsToken, new BoundAssignmentOperator(SyntaxKind.PlusEqualsToken, BoundAssignmentOperatorKind.AdditionInline) },
        { SyntaxKind.MinusEqualsToken, new BoundAssignmentOperator(SyntaxKind.MinusEqualsToken, BoundAssignmentOperatorKind.SubstractionInline) },
        { SyntaxKind.StarEqualsToken, new BoundAssignmentOperator(SyntaxKind.StarEqualsToken, BoundAssignmentOperatorKind.MultiplicationInline) },
        { SyntaxKind.SlashEqualsToken, new BoundAssignmentOperator(SyntaxKind.SlashEqualsToken, BoundAssignmentOperatorKind.DivisionInline) }
    };

    internal static BoundAssignmentOperator MatchAssignmentOperator(SyntaxKind syntaxKind)
        => supportedAssignmentOperators.GetValueOrDefault(syntaxKind, null);

    public BoundExpression Left { get; internal init; }
    public BoundAssignmentOperator Operator { get; internal init; }
    public BoundExpression Right { get; internal init; }
    public override TypeSymbol ResultType => Right.ResultType;

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundAssignmentExpression(this);
}

public partial class Binder
{
    private BoundAssignmentExpression BindAssignmentExpression(AssignmentExpression assignmentExpression)
    {
        var boundAssignmentOperator = BoundAssignmentExpression.MatchAssignmentOperator(assignmentExpression.AssignmentOperator.Kind);
        var right = BindExpression(assignmentExpression.Right);

        if (assignmentExpression.Left is SimpleNameExpression simpleNameExpression)
        {
            var variableName = simpleNameExpression.CanonicalName;
            var variable = Scope.LookupVariable(variableName);

            if (variable == null)
            {
                if (boundAssignmentOperator.BoundAssignmentOperatorKind == BoundAssignmentExpression.BoundAssignmentOperatorKind.Assignment
                    && AllowVariableDeclarationInAssignment)
                {
                    variable = new ReplVariableSymbol()
                    {
                        AssignmentExpression = assignmentExpression,
                        BoundInitializer = right
                    };
                    Scope.DeclareVariable(variable);
                }
            }
            else if (variable.ReadOnly)
            {
                ReportDiagnostic(
                    new Diagnostic()
                    {
                        Message = $"Variable {variableName} is read-only",
                        Level = DiagnosticLevel.Error,
                        TextLocation = simpleNameExpression.IdentifierToken.GetTextLocation(),
                        ErrorCode = ErrorCode.ReadOnlyVariable
                    });
            }
            else if (!variable.Type.Equals(right.ResultType))
            {
                ReportDiagnostic(
                    new Diagnostic()
                    {
                        Message = $"Variable {variableName} cannot be assigned to type {right.ResultType}",
                        Level = DiagnosticLevel.Error,
                        TextLocation = simpleNameExpression.IdentifierToken.GetTextLocation(),
                        ErrorCode = ErrorCode.TypeMismatch
                    });
            }
        }

        var left = BindExpression(assignmentExpression.Left);
        if (!left.LValue)
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = $"The left-hand side of an assignment must be a variable, property or indexer",
                    Level = DiagnosticLevel.Error,
                    TextLocation = default,
                    ErrorCode = ErrorCode.NotAnLValue
                });
        }

        return BoundNodeFactory.CreateBoundAssignmentExpression(
            syntaxNode: assignmentExpression,
            left: left,
            right: right,
            @operator: boundAssignmentOperator);
    }
}
