﻿using System;
using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundModule : IDiagnosable
{
    private readonly Binder binder;
    private readonly List<BoundMember> boundMembers = new();

    public IReadOnlyList<SyntaxTree> SyntaxTrees { get; private init; }
    public IReadOnlyList<BoundMember> BoundMembers => boundMembers;

    // make ctor private
    private BoundModule()
    {
        binder = Binder.CreateModuleBinder();
    }

    private void BindSyntaxTrees()
    {
        var members = SyntaxTrees.SelectMany(tree => tree.Members);
        foreach (var functionDeclarationMember in members.OfType<FunctionDeclarationMember>())
        {
            binder.Scope.DeclareFunction(FunctionSymbol.FromFunctionDeclarationMember(functionDeclarationMember));
        }

        boundMembers.AddRange(members.Select(m => binder.BindMember(m)));
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

    public IEnumerable<Diagnostic> GetDiagnostics()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        diagnosticBuilder.AddRange(BoundMembers);
        return diagnosticBuilder.Build();
    }
}
