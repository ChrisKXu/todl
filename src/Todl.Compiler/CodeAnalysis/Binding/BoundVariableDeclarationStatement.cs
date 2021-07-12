using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundVariableDeclarationStatement : BoundStatement
    {
        public VariableSymbol Variable { get; }
        public BoundExpression InitializerExpression { get; }

        public BoundVariableDeclarationStatement(VariableSymbol variable, BoundExpression initializerExpression)
        {
            this.Variable = variable;
            this.InitializerExpression = initializerExpression;
        }
    }

    public sealed partial class Binder
    {
        private BoundVariableDeclarationStatement BindVariableDeclarationStatement(
            BoundScope scope,
            VariableDeclarationStatement variableDeclarationStatement)
        {
            var initializerExpression = this.BindExpression(scope, variableDeclarationStatement.InitializerExpression);
            var variable = new VariableSymbol(
                name: variableDeclarationStatement.IdentifierToken.Text.ToString(),
                readOnly: variableDeclarationStatement.AssignmentToken.Kind == SyntaxKind.ConstKeywordToken,
                type: initializerExpression.ResultType);

            scope.DeclareVariable(variable);

            return new BoundVariableDeclarationStatement(variable, initializerExpression);
        }
    }
}
