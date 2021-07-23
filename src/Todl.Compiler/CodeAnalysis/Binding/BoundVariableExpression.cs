using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundVariableExpression : BoundExpression
    {
        public VariableSymbol Variable { get; internal init; }
        public override TypeSymbol ResultType => Variable.Type;
        public override bool LValue => true;
    }

    public sealed partial class Binder
    {
        private BoundExpression BindNameExpression(BoundScope scope, NameExpression nameExpression)
        {
            var name = nameExpression.IdentifierToken.Text.ToString();
            if (loadedNamespaces.Contains(name))
            {
                return new BoundNamespaceExpression() { Namespace = name };
            }

            var variable = scope.LookupVariable(name);
            if (variable == null)
            {
                return ReportErrorExpression(
                    new Diagnostic(
                        message: $"Undeclared variable {nameExpression.IdentifierToken.Text}",
                        level: DiagnosticLevel.Error,
                        textLocation: nameExpression.IdentifierToken.GetTextLocation()));
            }

            return new BoundVariableExpression() { Variable = variable };
        }
    }
}
