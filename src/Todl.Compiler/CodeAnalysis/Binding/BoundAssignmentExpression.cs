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

        public VariableSymbol Variable { get; }
        public BoundAssignmentOperator Operator { get; }
        public BoundExpression BoundExpression { get; }
        public override TypeSymbol ResultType => this.BoundExpression.ResultType;

        public BoundAssignmentExpression(VariableSymbol variable, BoundAssignmentOperator boundAssignmentOperator, BoundExpression boundExpression)
        {
            this.Variable = variable;
            this.Operator = boundAssignmentOperator;
            this.BoundExpression = boundExpression;
        }
    }

    public sealed partial class Binder
    {
        private BoundExpression BindAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            var variableName = assignmentExpression.IdentifierToken.Text.ToString();
            var variable = this.boundScope.LookupVariable(variableName);
            var boundAssignmentOperator = BoundAssignmentExpression.MatchAssignmentOperator(assignmentExpression.AssignmentOperator.Kind);
            var boundExpression = this.BindExpression(assignmentExpression.Expression);

            if (variable == null)
            {
                if (this.binderFlags.Includes(BinderFlags.AllowVariableDeclarationInAssignment))
                {
                    variable = new VariableSymbol(variableName, false, boundExpression.ResultType);
                    this.boundScope.DeclareVariable(variable);
                }
                else
                {
                    return this.ReportErrorExpression(
                        new Diagnostic(
                            message: $"Undeclared variable {assignmentExpression.IdentifierToken.Text}",
                            level: DiagnosticLevel.Error,
                            textLocation: assignmentExpression.IdentifierToken.GetTextLocation()));
                }
            }
            else if (variable.ReadOnly)
            {
                return this.ReportErrorExpression(
                    new Diagnostic(
                        message: $"Variable {assignmentExpression.IdentifierToken.Text} is read-only",
                        level: DiagnosticLevel.Error,
                        textLocation: assignmentExpression.IdentifierToken.GetTextLocation()));
            }
            else if (!variable.Type.Equals(boundExpression.ResultType))
            {
                return this.ReportErrorExpression(
                    new Diagnostic(
                        message: $"Variable {assignmentExpression.IdentifierToken.Text} cannot be assigned to type {boundExpression.ResultType}",
                        level: DiagnosticLevel.Error,
                        textLocation: assignmentExpression.IdentifierToken.GetTextLocation()));
            }

            return new BoundAssignmentExpression(
                variable: variable,
                boundAssignmentOperator: boundAssignmentOperator,
                boundExpression: boundExpression);
        }
    }
}
