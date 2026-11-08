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

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundAssignmentExpression(this);
}

public partial class Binder
{
    private BoundAssignmentExpression BindAssignmentExpression(AssignmentExpression assignmentExpression)
    {
        var boundAssignmentOperator = BoundAssignmentExpression.MatchAssignmentOperator(assignmentExpression.AssignmentOperator.Kind);
        var right = BindExpression(assignmentExpression.Right);

        // A REPL assignment declares a new variable; this must happen before the
        // left side is bound so the simple name resolves instead of reporting an
        // undeclared variable.
        if (assignmentExpression.Left is SimpleNameExpression simpleNameExpression
            && Scope.LookupVariable(simpleNameExpression.CanonicalName) is null
            && boundAssignmentOperator.BoundAssignmentOperatorKind == BoundAssignmentExpression.BoundAssignmentOperatorKind.Assignment
            && AllowVariableDeclarationInAssignment)
        {
            Scope.DeclareVariable(new ReplVariableSymbol()
            {
                AssignmentExpression = assignmentExpression,
                BoundInitializer = right
            });
        }

        var left = BindExpression(assignmentExpression.Left);
        CheckAssignmentTarget(left, right);

        return BoundNodeFactory.CreateBoundAssignmentExpression(
            syntaxNode: assignmentExpression,
            left: left,
            right: right,
            @operator: boundAssignmentOperator,
            resultType: right.ResultType);
    }

    private void CheckAssignmentTarget(BoundExpression left, BoundExpression right)
    {
        if (!left.LValue)
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = "The left-hand side of an assignment must be a variable, property or indexer",
                    Level = DiagnosticLevel.Error,
                    TextLocation = left.SyntaxNode.GetTextLocation(),
                    ErrorCode = ErrorCode.NotAnLValue
                });

            return;
        }

        // Invalid targets already reported their own diagnostic and carry no type.
        if (left.ResultType is null || right.ResultType is null)
        {
            return;
        }

        // Accessibility has already been reported; don't pile on more errors.
        if (left is BoundMemberAccessExpression { IsPublic: false }
            or BoundIndexerAccessExpression { IsPublic: false })
        {
            return;
        }

        var targetDescription = GetAssignmentTargetDescription(left);

        if (left.ReadOnly)
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = $"{targetDescription} is read-only",
                    Level = DiagnosticLevel.Error,
                    TextLocation = left.SyntaxNode.GetTextLocation(),
                    ErrorCode = ErrorCode.ReadOnlyVariable
                });

            return;
        }

        if (!IsAssignmentCompatible(left.ResultType, right.ResultType))
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = $"{targetDescription} cannot be assigned to type {right.ResultType}",
                    Level = DiagnosticLevel.Error,
                    TextLocation = left.SyntaxNode.GetTextLocation(),
                    ErrorCode = ErrorCode.TypeMismatch
                });
        }
    }

    // Identity, or a reference conversion: storing a derived reference in a base
    // reference needs no IL. Value types can't be converted here because the
    // emitter has no conversion/boxing support yet.
    private static bool IsAssignmentCompatible(TypeSymbol targetType, TypeSymbol valueType)
    {
        if (targetType.Equals(valueType))
        {
            return true;
        }

        return targetType is ClrTypeSymbol targetClrType
            && valueType is ClrTypeSymbol valueClrType
            && targetClrType.IsReferenceType
            && valueClrType.IsReferenceType
            && targetClrType.ClrType.IsAssignableFrom(valueClrType.ClrType);
    }

    private static string GetAssignmentTargetDescription(BoundExpression target)
        => target switch
        {
            BoundVariableExpression variableExpression => $"Variable {variableExpression.Variable.Name}",
            BoundMemberAccessExpression memberAccessExpression => $"Member {memberAccessExpression.MemberName}",
            BoundArrayElementAccessExpression => "Array element",
            BoundIndexerAccessExpression indexerAccessExpression
                => $"Indexer of type {indexerAccessExpression.BoundBaseExpression.ResultType}",
            _ => "Assignment target"
        };
}
