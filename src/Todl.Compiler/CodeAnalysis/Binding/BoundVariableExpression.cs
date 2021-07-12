using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundVariableExpression : BoundExpression
    {
        public VariableSymbol Variable { get; }
        public override TypeSymbol ResultType => this.Variable.Type;

        public BoundVariableExpression(VariableSymbol variable)
        {
            this.Variable = variable;
        }
    }

    public sealed partial class Binder
    {
        private BoundExpression BindNameExpression(BoundScope scope, NameExpression nameExpression)
        {
            var variable = scope.LookupVariable(nameExpression.IdentifierToken.Text.ToString());
            if (variable == null)
            {
                return this.ReportErrorExpression(
                    new Diagnostic(
                        message: $"Undeclared variable {nameExpression.IdentifierToken.Text}",
                        level: DiagnosticLevel.Error,
                        textLocation: nameExpression.IdentifierToken.GetTextLocation()));
            }

            return new BoundVariableExpression(variable);
        }
    }
}
