using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundAssignmentExpression : BoundExpression
    {
        public sealed class BoundAssignmentOperator
        {
            public SyntaxKind SyntaxKind { get; }
            public BoundAssignmentOperatorKind BoundAssignmentOperatorKind { get; }

            public BoundAssignmentOperator(SyntaxKind syntaxKind, BoundAssignmentOperatorKind boundAssignmentOperatorKind)
            {
                this.SyntaxKind = syntaxKind;
                this.BoundAssignmentOperatorKind = boundAssignmentOperatorKind;
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
            => BoundAssignmentExpression.supportedAssignmentOperators.GetValueOrDefault(syntaxKind, null);

        public BoundExpression Left { get; }
        public BoundAssignmentOperator Operator { get; }
        public BoundExpression Right { get; }
        public override TypeSymbol ResultType => Right.ResultType;

        public BoundAssignmentExpression(BoundExpression left, BoundAssignmentOperator boundAssignmentOperator, BoundExpression right)
        {
            this.Left = left;
            this.Operator = boundAssignmentOperator;
            this.Right = right;
        }
    }

    public sealed partial class Binder
    {
        private BoundExpression BindAssignmentExpression(BoundScope scope, AssignmentExpression assignmentExpression)
        {
            var boundAssignmentOperator = BoundAssignmentExpression.MatchAssignmentOperator(assignmentExpression.AssignmentOperator.Kind);
            var right = BindExpression(scope, assignmentExpression.Right);

            if (assignmentExpression.Left is NameExpression nameExpression)
            {
                var variableName = nameExpression.IdentifierToken.Text.ToString();
                var variable = scope.LookupVariable(variableName);

                if (variable == null)
                {
                    if (boundAssignmentOperator.BoundAssignmentOperatorKind == BoundAssignmentExpression.BoundAssignmentOperatorKind.Assignment
                        && this.binderFlags.Includes(BinderFlags.AllowVariableDeclarationInAssignment))
                    {
                        variable = new VariableSymbol(variableName, false, right.ResultType);
                        scope.DeclareVariable(variable);
                    }
                    else
                    {
                        return ReportErrorExpression(
                            new Diagnostic()
                            {
                                Message = $"Undeclared variable {variableName}",
                                Level = DiagnosticLevel.Error,
                                TextLocation = nameExpression.IdentifierToken.GetTextLocation(),
                                ErrorCode = ErrorCode.UndeclaredVariable
                            });
                    }
                }
                else if (variable.ReadOnly)
                {
                    return ReportErrorExpression(
                        new Diagnostic()
                        {
                            Message = $"Variable {variableName} is read-only",
                            Level = DiagnosticLevel.Error,
                            TextLocation = nameExpression.IdentifierToken.GetTextLocation(),
                            ErrorCode = ErrorCode.ReadOnlyVariable
                        });
                }
                else if (!variable.Type.Equals(right.ResultType))
                {
                    return ReportErrorExpression(
                        new Diagnostic()
                        {
                            Message = $"Variable {variableName} cannot be assigned to type {right.ResultType}",
                            Level = DiagnosticLevel.Error,
                            TextLocation = nameExpression.IdentifierToken.GetTextLocation(),
                            ErrorCode = ErrorCode.TypeMismatch
                        });
                }
            }

            var left = BindExpression(scope, assignmentExpression.Left);
            if (!left.LValue)
            {
                return this.ReportErrorExpression(
                    new Diagnostic()
                    {
                        Message = $"The left-hand side of an assignment must be a variable, property or indexer",
                        Level = DiagnosticLevel.Error,
                        TextLocation = default,
                        ErrorCode = ErrorCode.NotAnLValue
                    });
            }

            return new BoundAssignmentExpression(
                left: left,
                boundAssignmentOperator: boundAssignmentOperator,
                right: right);
        }
    }
}
