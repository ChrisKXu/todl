using System;
using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Binding.ControlFlowAnalysis;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

internal sealed class BoundModule : IDiagnosable
{
    public IReadOnlyCollection<SyntaxTree> SyntaxTrees { get; private init; }
    public BoundEntryPointTypeDefinition EntryPointType { get; private init; }
    public BoundFunctionMember EntryPoint => EntryPointType.EntryPointFunctionMember;
    public DiagnosticBag.Builder DiagnosticBuilder { get; private init; }

    public static BoundModule Create(
        ClrTypeCache clrTypeCache,
        IReadOnlyList<SyntaxTree> syntaxTrees)
    {
        syntaxTrees ??= Array.Empty<SyntaxTree>();
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var binder = Binder.CreateModuleBinder(clrTypeCache, diagnosticBuilder);
        var entryPointType = binder.BindEntryPointTypeDefinition(syntaxTrees);

        var controlFlowAnalyzer = new ControlFlowAnalyzer(diagnosticBuilder);
        entryPointType.Accept(controlFlowAnalyzer);

        var boundNodeVisitors = new BoundNodeVisitor[]
        {
            new ConstantFoldingBoundNodeVisitor(binder.ConstantValueFactory)
        };

        foreach (var v in boundNodeVisitors)
        {
            entryPointType = (BoundEntryPointTypeDefinition)v.VisitBoundTypeDefinition(entryPointType);
        }

        return new()
        {
            SyntaxTrees = syntaxTrees,
            EntryPointType = entryPointType,
            DiagnosticBuilder = diagnosticBuilder
        };
    }

    public IEnumerable<Diagnostic> GetDiagnostics()
        => DiagnosticBuilder.Build();
}
