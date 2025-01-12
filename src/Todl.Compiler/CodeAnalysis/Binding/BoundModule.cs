using System;
using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Binding.ControlFlowAnalysis;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding;

internal sealed class BoundModule
{
    public IReadOnlyCollection<SyntaxTree> SyntaxTrees { get; private init; }
    public BoundEntryPointTypeDefinition EntryPointType { get; private init; }
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

        var controlFlowAnalyzer = new ControlFlowAnalyzer(diagnosticBuilder);
        var constantFoldingBoundNodeVisitor = new ConstantFoldingBoundNodeVisitor(binder.ConstantValueFactory);

        entryPointType.Accept(controlFlowAnalyzer);
        entryPointType.Accept(constantFoldingBoundNodeVisitor);

        return new()
        {
            SyntaxTrees = syntaxTrees,
            EntryPointType = entryPointType,
            DiagnosticBuilder = diagnosticBuilder
        };
    }
}
