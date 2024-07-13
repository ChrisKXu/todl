using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[BoundNode]
internal sealed class BoundVariableMember : BoundMember
{
    public BoundVariableDeclarationStatement BoundVariableDeclarationStatement { get; internal init; }

    public override BoundNode Accept(BoundTreeVisitor visitor) => visitor.VisitBoundVariableMember(this);
}

public partial class Binder
{
    private BoundVariableMember BindVariableDeclarationMember(VariableDeclarationMember variableDeclarationMember)
        => BoundNodeFactory.CreateBoundVariableMember(
            syntaxNode: variableDeclarationMember,
            boundVariableDeclarationStatement: BindVariableDeclarationStatement(variableDeclarationMember.VariableDeclarationStatement));
}
