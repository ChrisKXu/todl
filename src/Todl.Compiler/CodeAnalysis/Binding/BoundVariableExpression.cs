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
        public override bool Constant => Variable.Constant;
    }

    public partial class Binder
    {
        private BoundExpression BindNameExpression(NameExpression nameExpression)
        {
            var name = nameExpression.Text.ToString();
            var type = nameExpression.SyntaxTree.ClrTypeCacheView.ResolveType(nameExpression);

            if (type != null)
            {
                return BoundNodeFactory.CreateBoundTypeExpression(
                    syntaxNode: nameExpression,
                    targetType: type);
            }

            var diagnosticBuilder = new DiagnosticBag.Builder();
            var variable = Scope.LookupVariable(name);
            if (variable == null)
            {
                diagnosticBuilder.Add(
                    new Diagnostic()
                    {
                        Message = $"Undeclared variable {nameExpression.Text}",
                        Level = DiagnosticLevel.Error,
                        TextLocation = nameExpression.SyntaxTokens[0].GetTextLocation(),
                        ErrorCode = ErrorCode.UndeclaredVariable
                    });
            }

            return BoundNodeFactory.CreateBoundVariableExpression(
                syntaxNode: nameExpression,
                variable: variable,
                diagnosticBuilder: diagnosticBuilder);
        }
    }
}
