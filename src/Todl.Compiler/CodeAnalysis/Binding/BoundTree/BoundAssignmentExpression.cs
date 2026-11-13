﻿using System;
using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundAssignmentExpression : BoundExpression
{
    public sealed record BoundAssignmentOperator(SyntaxKind SyntaxKind)
    {
        internal SyntaxKind GetBinarySyntaxKind() => SyntaxKind switch
        {
            SyntaxKind.PlusEqualsToken => SyntaxKind.PlusToken,
            SyntaxKind.MinusEqualsToken => SyntaxKind.MinusToken,
            SyntaxKind.StarEqualsToken => SyntaxKind.StarToken,
            SyntaxKind.SlashEqualsToken => SyntaxKind.SlashToken,
            _ => throw new InvalidOperationException("Assignment operator has no binary counterpart")
        };
    }

    private static readonly IReadOnlyDictionary<SyntaxKind, BoundAssignmentOperator> supportedAssignmentOperators = new Dictionary<SyntaxKind, BoundAssignmentOperator>()
    {
        { SyntaxKind.EqualsToken, new BoundAssignmentOperator(SyntaxKind.EqualsToken) },
        { SyntaxKind.PlusEqualsToken, new BoundAssignmentOperator(SyntaxKind.PlusEqualsToken) },
        { SyntaxKind.MinusEqualsToken, new BoundAssignmentOperator(SyntaxKind.MinusEqualsToken) },
        { SyntaxKind.StarEqualsToken, new BoundAssignmentOperator(SyntaxKind.StarEqualsToken) },
        { SyntaxKind.SlashEqualsToken, new BoundAssignmentOperator(SyntaxKind.SlashEqualsToken) }
    };

    internal static BoundAssignmentOperator MatchAssignmentOperator(SyntaxKind syntaxKind)
        => supportedAssignmentOperators.GetValueOrDefault(syntaxKind, null);

    public BoundExpression Left { get; internal init; }
    public BoundAssignmentOperator Operator { get; internal init; }
    public BoundExpression Right { get; internal init; }

    public BoundBinaryOperator BinaryOperator
    {
        get
        {
            if (Operator.SyntaxKind == SyntaxKind.EqualsToken
                || Left.ResultType is null
                || Right.ResultType is null)
            {
                return null;
            }

            return BoundBinaryOperatorFactory.MatchBinaryOperator(
                Left.ResultType.SpecialType,
                Right.ResultType.SpecialType,
                Operator.GetBinarySyntaxKind());
        }
    }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundAssignmentExpression(this);
}

public partial class Binder
{
    private BoundAssignmentExpression BindAssignmentExpression(AssignmentExpression assignmentExpression)
    {
        var boundAssignmentOperator = BoundAssignmentExpression.MatchAssignmentOperator(assignmentExpression.AssignmentOperator.Kind);
        var isSimpleAssignment = boundAssignmentOperator.SyntaxKind == SyntaxKind.EqualsToken;
        var right = BindExpression(assignmentExpression.Right);

        // A REPL assignment declares a new variable; this must happen before the
        // left side is bound so the simple name resolves instead of reporting an
        // undeclared variable.
        if (assignmentExpression.Left is SimpleNameExpression simpleNameExpression
            && Scope.LookupVariable(simpleNameExpression.CanonicalName) is null
            && isSimpleAssignment
            && AllowVariableDeclarationInAssignment)
        {
            Scope.DeclareVariable(new ReplVariableSymbol()
            {
                AssignmentExpression = assignmentExpression,
                BoundInitializer = right
            });
        }

        var left = BindExpression(assignmentExpression.Left);
        var isTargetWritable = CheckAssignmentTarget(left);

        return isSimpleAssignment
            ? BindSimpleAssignmentExpression(assignmentExpression, boundAssignmentOperator, left, right, isTargetWritable)
            : BindCompoundAssignmentExpression(assignmentExpression, boundAssignmentOperator, left, right, isTargetWritable);
    }

    private BoundAssignmentExpression BindSimpleAssignmentExpression(
        AssignmentExpression assignmentExpression,
        BoundAssignmentExpression.BoundAssignmentOperator boundAssignmentOperator,
        BoundExpression left,
        BoundExpression right,
        bool isTargetWritable)
    {
        if (isTargetWritable && right.ResultType is not null)
        {
            CheckAssignmentType(left, right.ResultType);
        }

        return BoundNodeFactory.CreateBoundAssignmentExpression(
            syntaxNode: assignmentExpression,
            left: left,
            right: right,
            @operator: boundAssignmentOperator,
            resultType: right.ResultType);
    }

    private BoundAssignmentExpression BindCompoundAssignmentExpression(
        AssignmentExpression assignmentExpression,
        BoundAssignmentExpression.BoundAssignmentOperator boundAssignmentOperator,
        BoundExpression left,
        BoundExpression right,
        bool isTargetWritable)
    {
        var resultType = default(TypeSymbol);

        if (isTargetWritable && left.ResultType is not null && right.ResultType is not null)
        {
            var binaryOperator = BoundBinaryOperatorFactory.MatchBinaryOperator(
                left.ResultType.SpecialType,
                right.ResultType.SpecialType,
                boundAssignmentOperator.GetBinarySyntaxKind());

            if (binaryOperator is null)
            {
                ReportDiagnostic(
                    new Diagnostic()
                    {
                        Message = $"Operator {assignmentExpression.AssignmentOperator.Text} is not supported on types {left.ResultType.Name} and {right.ResultType.Name}",
                        Level = DiagnosticLevel.Error,
                        TextLocation = assignmentExpression.GetTextLocation(assignmentExpression.AssignmentOperator.Span),
                        ErrorCode = ErrorCode.UnsupportedOperator
                    });
            }
            else
            {
                if (binaryOperator.BoundBinaryOperatorKind == BoundBinaryOperatorKind.StringConcatenation)
                {
                    // The target is also the left operand, so it cannot be replaced by a ToString() call.
                    // A valid concatenating compound assignment always targets string already.
                    right = ConvertToStringOperand(right, ClrTypeCache);
                }

                resultType = ClrTypeCache.ResolveSpecialType(binaryOperator.ResultType);
            }
        }

        if (isTargetWritable && resultType is not null)
        {
            CheckAssignmentType(left, resultType);
        }

        return BoundNodeFactory.CreateBoundAssignmentExpression(
            syntaxNode: assignmentExpression,
            left: left,
            right: right,
            @operator: boundAssignmentOperator,
            resultType: resultType);
    }

    private bool CheckAssignmentTarget(BoundExpression left)
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

            return false;
        }

        // Invalid targets already reported their own diagnostic and carry no type.
        if (left.ResultType is null)
        {
            return false;
        }

        // Accessibility has already been reported; don't pile on more errors.
        if (left is BoundMemberAccessExpression { IsPublic: false }
            or BoundIndexerAccessExpression { IsPublic: false })
        {
            return false;
        }

        if (left.ReadOnly)
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = $"{GetAssignmentTargetDescription(left)} is read-only",
                    Level = DiagnosticLevel.Error,
                    TextLocation = left.SyntaxNode.GetTextLocation(),
                    ErrorCode = ErrorCode.ReadOnlyVariable
                });

            return false;
        }

        return true;
    }

    private void CheckAssignmentType(BoundExpression left, TypeSymbol valueType)
    {
        if (!IsAssignmentCompatible(left.ResultType, valueType))
        {
            ReportDiagnostic(
                new Diagnostic()
                {
                    Message = $"{GetAssignmentTargetDescription(left)} cannot be assigned to type {valueType}",
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
