using System.Collections.Generic;
using System.Linq;

namespace Todl.Compiler.CodeAnalysis.Binding;

public abstract class BoundTodlTypeDefinition : BoundNode
{
    public string Name { get; internal init; }
    public IReadOnlyList<BoundMember> BoundMembers { get; internal init; }
    public virtual bool IsStatic => false;
    public virtual bool IsGeneratedType => false;

    public IEnumerable<BoundFunctionMember> Functions
        => BoundMembers.OfType<BoundFunctionMember>();

    public IEnumerable<BoundVariableMember> Variables
        => BoundMembers.OfType<BoundVariableMember>();

    public bool IsPublic => char.IsUpper(Name[0]);
}
