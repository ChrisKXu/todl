using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public abstract class BoundStatement : BoundNode { }

    public sealed partial class Binder
    {
        public BoundStatement BindStatement(BoundScope scope, Statement statement)
        {
            return statement switch
            {
                ExpressionStatement expressionStatement => BindExpressionStatement(scope, expressionStatement),
                BlockStatement blockStatement => BindBlockStatement(scope.CreateChildScope(BoundScopeKind.BlockStatement), blockStatement),
                VariableDeclarationStatement variableDeclarationStatement => BindVariableDeclarationStatement(scope, variableDeclarationStatement),
                ReturnStatement returnStatement => BindReturnStatement(scope, returnStatement),
                _ => null
            };
        }
    }
}
