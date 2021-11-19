using System;
using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundModule
{
    private readonly Binder binder = new Binder(BinderFlags.None);
    private readonly BoundScope boundScope = BoundScope.GlobalScope.CreateChildScope(BoundScopeKind.Module);
    private readonly List<BoundMember> boundMembers = new();

    public IReadOnlyList<SyntaxTree> SyntaxTrees { get; private init; }
    public IReadOnlyList<BoundMember> BoundMembers => boundMembers;

    // make ctor private
    private BoundModule() { }

    private void BindSyntaxTrees()
    {
        var members = SyntaxTrees.SelectMany(tree => tree.Members);
        boundMembers.AddRange(members.Select(m => binder.BindMember(boundScope, m)));
    }

    public static BoundModule Create(
        IReadOnlyList<SyntaxTree> syntaxTrees)
    {
        syntaxTrees ??= Array.Empty<SyntaxTree>();

        var boundModule = new BoundModule()
        {
            SyntaxTrees = syntaxTrees
        };

        boundModule.BindSyntaxTrees();

        return boundModule;
    }
}
