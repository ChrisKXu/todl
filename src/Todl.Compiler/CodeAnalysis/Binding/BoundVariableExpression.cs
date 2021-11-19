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
            var name = nameExpression.Text.ToString();
            var type = nameExpression.SyntaxTree.ClrTypeCacheView.ResolveType(name);

            if (type != null)
            {
                return new BoundTypeExpression() { ResultType = ClrTypeSymbol.MapClrType(type) };
            }

            var variable = scope.LookupVariable(name);
            if (variable == null)
            {
                return ReportErrorExpression(
                    new Diagnostic()
                    {
                        Message = $"Undeclared variable {nameExpression.Text}",
                        Level = DiagnosticLevel.Error,
                        TextLocation = nameExpression.SyntaxTokens[0].GetTextLocation(),
                        ErrorCode = ErrorCode.UndeclaredVariable
                    });
            }

            return new BoundVariableExpression() { Variable = variable };
        }
    }
}
