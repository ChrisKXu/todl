using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
public sealed class BoundVariableMember : BoundMember
{
    public BoundVariableDeclarationStatement BoundVariableDeclarationStatement { get; internal init; }
}

public partial class Binder
{
    private BoundVariableMember BindVariableDeclarationMember(VariableDeclarationMember variableDeclarationMember)
        => BoundNodeFactory.CreateBoundVariableMember(
            syntaxNode: variableDeclarationMember,
            boundVariableDeclarationStatement: BindVariableDeclarationStatement(variableDeclarationMember.VariableDeclarationStatement));
}
