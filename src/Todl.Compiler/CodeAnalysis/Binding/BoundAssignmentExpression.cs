using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundAssignmentExpression : BoundExpression
    {
        public VariableSymbol Variable { get; }
        public BoundExpression BoundExpression { get; }
        public override TypeSymbol ResultType => this.BoundExpression.ResultType;

        public BoundAssignmentExpression(VariableSymbol variable, BoundExpression boundExpression)
        {
            this.Variable = variable;
            this.BoundExpression = boundExpression;
        }
    }

    public sealed partial class Binder
    {
        public BoundExpression BindAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            var variableName = assignmentExpression.IdentifierToken.Text.ToString();
            var variable = this.boundScope.LookupVariable(variableName);
            var boundExpression = this.BindExpression(assignmentExpression.Expression);

            if (variable == null)
            {
                this.diagnostics.Add(
                    new Diagnostic(
                        message: $"Undeclared variable {assignmentExpression.IdentifierToken.Text}",
                        level: DiagnosticLevel.Error,
                        textLocation: assignmentExpression.IdentifierToken.GetTextLocation()));
                variable = new VariableSymbol(variableName, false, boundExpression.ResultType);
                this.boundScope.DeclareVariable(variable);
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
                        message: $"Variable {assignmentExpression.IdentifierToken.Text} is read-only",
                        level: DiagnosticLevel.Error,
                        textLocation: assignmentExpression.IdentifierToken.GetTextLocation()));
            }

            return new BoundAssignmentExpression(
                variable: variable,
                boundExpression: boundExpression);
        }
    }
}
