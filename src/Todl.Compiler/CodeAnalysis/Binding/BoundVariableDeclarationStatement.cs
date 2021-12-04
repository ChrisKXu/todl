using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundVariableDeclarationStatement : BoundStatement
    {
        public VariableSymbol Variable { get; internal init; }
        public BoundExpression InitializerExpression { get; internal init; }
    }

    public partial class Binder
    {
        private BoundVariableDeclarationStatement BindVariableDeclarationStatement(
            VariableDeclarationStatement variableDeclarationStatement)
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var initializerExpression = BindExpression(variableDeclarationStatement.InitializerExpression);
            diagnosticBuilder.Add(initializerExpression);

            var variable = new VariableSymbol(
                name: variableDeclarationStatement.IdentifierToken.Text.ToString(),
                readOnly: variableDeclarationStatement.AssignmentToken.Kind == SyntaxKind.ConstKeywordToken,
                type: initializerExpression.ResultType);

            Scope.DeclareVariable(variable);

            return new()
            {
                SyntaxNode = variableDeclarationStatement,
                Variable = variable,
                InitializerExpression = initializerExpression,
                DiagnosticBuilder = diagnosticBuilder
            };
        }
    }
}
