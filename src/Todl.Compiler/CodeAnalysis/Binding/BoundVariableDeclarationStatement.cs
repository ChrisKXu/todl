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
            var initializerExpression = BindExpression(variableDeclarationStatement.InitializerExpression);
            var variable = new VariableSymbol(
                name: variableDeclarationStatement.IdentifierToken.Text.ToString(),
                readOnly: variableDeclarationStatement.AssignmentToken.Kind == SyntaxKind.ConstKeywordToken,
                type: initializerExpression.ResultType);

            Scope.DeclareVariable(variable);

            return BoundNodeFactory.CreateBoundVariableDeclarationStatement(
                syntaxNode: variableDeclarationStatement,
                variable: variable,
                initializerExpression: initializerExpression);
        }
    }
}
