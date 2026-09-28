using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding.ControlFlowAnalysis;

internal sealed class ControlFlowAnalyzer : BoundTreeWalker
{
    private readonly DiagnosticBag.Builder diagnosticBuilder;

    public ControlFlowAnalyzer(DiagnosticBag.Builder diagnosticBuilder)
    {
        this.diagnosticBuilder = diagnosticBuilder;
    }

    public override BoundNode VisitBoundFunctionMember(BoundFunctionMember boundFunctionMember)
    {
        var controlFlowGraph = ControlFlowGraph.Create(boundFunctionMember);

        if (boundFunctionMember.ReturnType.SpecialType != SpecialType.ClrVoid)
        {
            AllPathsShouldReturn(controlFlowGraph, boundFunctionMember);
        }

        AllBlocksShouldBeReachable(controlFlowGraph);

        return boundFunctionMember;
    }

    private void AllPathsShouldReturn(
        ControlFlowGraph controlFlowGraph,
        BoundFunctionMember boundFunctionMember)
    {
        var start = controlFlowGraph.StartBlock;
        var end = controlFlowGraph.EndBlock;

        var endIsReachable = end.Incoming.Any(i => i.From != start);

        if (!endIsReachable || end.Incoming.Any(i => i.From.Reachable && !i.From.IsReturn))
        {
            diagnosticBuilder.Add(new Diagnostic()
            {
                Message = "Not all paths return a value",
                ErrorCode = ErrorCode.NotAllPathsReturn,
                Level = DiagnosticLevel.Error,
                TextLocation = boundFunctionMember.FunctionSymbol.FunctionDeclarationMember.GetTextLocation(boundFunctionMember.FunctionSymbol.FunctionDeclarationMember.Name.Span)
            });
        }
    }

    private void AllBlocksShouldBeReachable(ControlFlowGraph controlFlowGraph)
    {
        var unreachableBlocks = controlFlowGraph.Blocks
            .Where(block => !block.Reachable
                && !block.Equals(controlFlowGraph.StartBlock)
                && !block.Equals(controlFlowGraph.EndBlock))
            .ToList();

        if (!unreachableBlocks.Any())
        {
            return;
        }

        // Blocks chained only through other unreachable blocks share a root cause and get one diagnostic.
        var parent = unreachableBlocks.ToDictionary(block => block, block => block);

        ControlFlowGraph.BasicBlock Find(ControlFlowGraph.BasicBlock block)
        {
            while (parent[block] != block)
            {
                parent[block] = parent[parent[block]];
                block = parent[block];
            }
            return block;
        }

        foreach (var branch in controlFlowGraph.Branches)
        {
            if (parent.ContainsKey(branch.From) && parent.ContainsKey(branch.To))
            {
                var fromRoot = Find(branch.From);
                var toRoot = Find(branch.To);
                if (fromRoot != toRoot)
                {
                    parent[fromRoot] = toRoot;
                }
            }
        }

        foreach (var region in unreachableBlocks.GroupBy(Find))
        {
            if (!TryGetUnreachableRegionLocation(region, out var textLocation))
            {
                continue;
            }

            diagnosticBuilder.Add(new Diagnostic()
            {
                Message = "Unreachable code",
                ErrorCode = ErrorCode.UnreachableCode,
                Level = DiagnosticLevel.Warning,
                TextLocation = textLocation
            });
        }
    }

    private static bool TryGetUnreachableRegionLocation(
        IEnumerable<ControlFlowGraph.BasicBlock> region,
        out TextLocation textLocation)
    {
        foreach (var block in region)
        {
            var realStatement = block.Statements.FirstOrDefault(
                statement => statement is not BoundNoOpStatement && statement.SyntaxNode is not null);

            if (realStatement is not null)
            {
                textLocation = realStatement.SyntaxNode.GetTextLocation();
                return true;
            }
        }

        // Entirely synthesized region: point at the owning if/while statement, not its placeholders.
        var originatingStatement = region
            .Select(block => block.OriginatingStatement)
            .FirstOrDefault(statement => statement is not null);

        if (originatingStatement is not null)
        {
            textLocation = originatingStatement.SyntaxNode.GetTextLocation();
            return true;
        }

        textLocation = default;
        return false;
    }
}
