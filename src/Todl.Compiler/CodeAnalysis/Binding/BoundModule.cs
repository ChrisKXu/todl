using System;
using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Binding.ControlFlowAnalysis;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundModule : IDiagnosable
{
    public IReadOnlyCollection<SyntaxTree> SyntaxTrees { get; private init; }
    public BoundEntryPointTypeDefinition EntryPointType { get; private init; }
    public BoundFunctionMember EntryPoint => EntryPointType.EntryPointFunctionMember;

    public static BoundModule Create(
        ClrTypeCache clrTypeCache,
        IReadOnlyList<SyntaxTree> syntaxTrees)
    {
        syntaxTrees ??= Array.Empty<SyntaxTree>();
        var binder = Binder.CreateModuleBinder(clrTypeCache);
        var entryPointType = binder.BindEntryPointTypeDefinition(syntaxTrees);

        var boundNodeVisitors = new BoundNodeVisitor[]
        {
            new ControlFlowAnalyzer(),
            new ConstantFoldingBoundNodeVisitor(binder.ConstantValueFactory)
        };

        foreach (var v in boundNodeVisitors)
        {
            entryPointType = (BoundEntryPointTypeDefinition)v.VisitBoundTypeDefinition(entryPointType);
        }

        return new()
        {
            SyntaxTrees = syntaxTrees,
            EntryPointType = entryPointType
        };
    }

    public IEnumerable<Diagnostic> GetDiagnostics()
        => EntryPointType.GetDiagnostics();
}
