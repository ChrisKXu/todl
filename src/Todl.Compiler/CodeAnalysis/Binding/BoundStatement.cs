using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public abstract class BoundStatement
    {

    }

    public sealed partial class Binder
    {
        public BoundStatement BindStatement(BoundScope scope, Statement statement)
        {
            return statement switch
            {
                ExpressionStatement expressionStatement => BindExpressionStatement(scope, expressionStatement),
                BlockStatement blockStatement => BindBlockStatement(scope, blockStatement),
                VariableDeclarationStatement variableDeclarationStatement => BindVariableDeclarationStatement(scope, variableDeclarationStatement),
                _ => null
            };
        }
    }
}
