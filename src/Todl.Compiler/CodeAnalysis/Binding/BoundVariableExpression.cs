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
            var name = nameExpression.QualifiedName.ToString();
            if (loadedNamespaces.Contains(name))
            {
                return new BoundNamespaceExpression() { Namespace = name };
            }

            if (loadedTypes.ContainsKey(name))
            {
                return new BoundTypeExpression() { ResultType = ClrTypeSymbol.MapClrType(loadedTypes[name]) };
            }

            var variable = scope.LookupVariable(name);
            if (variable == null)
            {
                return ReportErrorExpression(
                    new Diagnostic()
                    {
                        Message = $"Undeclared variable {nameExpression.QualifiedName}",
                        Level = DiagnosticLevel.Error,
                        TextLocation = nameExpression.SyntaxTokens[0].GetTextLocation(),
                        ErrorCode = ErrorCode.UndeclaredVariable
                    });
            }

            return new BoundVariableExpression() { Variable = variable };
        }
    }
}
