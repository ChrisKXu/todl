using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
public sealed class BoundVariableDeclarationStatement : BoundStatement
{
    public LocalVariableSymbol Variable { get; internal init; }
    public BoundExpression InitializerExpression { get; internal init; }
}

public partial class Binder
{
    private BoundVariableDeclarationStatement BindVariableDeclarationStatement(
        VariableDeclarationStatement variableDeclarationStatement)
    {
        var initializerExpression = BindExpression(variableDeclarationStatement.InitializerExpression);
        var variable = new LocalVariableSymbol()
        {
            VariableDeclarationStatement = variableDeclarationStatement,
            BoundInitializer = initializerExpression
        };

        Scope.DeclareVariable(variable);

        return BoundNodeFactory.CreateBoundVariableDeclarationStatement(
            syntaxNode: variableDeclarationStatement,
            variable: variable,
            initializerExpression: initializerExpression);
    }
}
