using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Binding.ControlFlowAnalysis;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

internal sealed class BoundModule
{
    public IReadOnlyCollection<SyntaxTree> SyntaxTrees { get; private init; }
    public BoundEntryPointTypeDefinition EntryPointType { get; private init; }
    public ImmutableArray<BoundTodlTypeDefinition> Types { get; private init; }
    public BoundFunctionMember EntryPoint => EntryPointType.EntryPointFunctionMember;
    public DiagnosticBag.Builder DiagnosticBuilder { get; private init; }

    public static BoundModule Create(
        ClrTypeCache clrTypeCache,
        IReadOnlyList<SyntaxTree> syntaxTrees,
        DiagnosticBag.Builder diagnosticBuilder)
    {
        syntaxTrees ??= Array.Empty<SyntaxTree>();
        var constantValueFactory = new ConstantValueFactory(clrTypeCache.BuiltInTypes);
        var binder = Binder.CreateModuleBinder(clrTypeCache, constantValueFactory, diagnosticBuilder);
        var entryPointType = binder.BindEntryPointTypeDefinition(syntaxTrees);

        // Order matters: constant folding must run before string concatenation lowering.
        var boundTreeVisitors = new BoundTreeVisitor[]
        {
            new ControlFlowAnalyzer(diagnosticBuilder),
            new ConstantFoldingBoundTreeRewriter(binder.ConstantValueFactory),
            new StringConcatenationLoweringBoundTreeRewriter(binder.ConstantValueFactory)
        };

        foreach (var boundTreeVisitor in boundTreeVisitors)
        {
            entryPointType = (BoundEntryPointTypeDefinition)entryPointType.Accept(boundTreeVisitor);
        }

        return new()
        {
            SyntaxTrees = syntaxTrees,
            EntryPointType = entryPointType,
            Types = ImmutableArray.Create<BoundTodlTypeDefinition>(entryPointType),
            DiagnosticBuilder = diagnosticBuilder
        };
    }
}
