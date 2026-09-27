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

        AllBlocksShouldBeReachable(controlFlowGraph, boundFunctionMember);

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

    private void AllBlocksShouldBeReachable(
        ControlFlowGraph controlFlowGraph,
        BoundFunctionMember boundFunctionMember)
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

        // Group unreachable blocks into regions: blocks chained together purely through
        // other unreachable blocks share the same root cause and get a single diagnostic,
        // instead of one warning per block in the dead subtree.
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
            diagnosticBuilder.Add(new Diagnostic()
            {
                Message = "Unreachable code",
                ErrorCode = ErrorCode.UnreachableCode,
                Level = DiagnosticLevel.Warning,
                TextLocation = GetUnreachableRegionLocation(region, boundFunctionMember)
            });
        }
    }

    private static TextLocation GetUnreachableRegionLocation(
        IEnumerable<ControlFlowGraph.BasicBlock> region,
        BoundFunctionMember boundFunctionMember)
    {
        foreach (var block in region)
        {
            var realStatement = block.Statements.FirstOrDefault(
                statement => statement is not BoundNoOpStatement && statement.SyntaxNode is not null);

            if (realStatement is not null)
            {
                return realStatement.SyntaxNode.GetTextLocation();
            }
        }

        // Entirely synthesized region (e.g. an empty block or a missing else clause): the
        // if/while statement that owns it is itself the unreachable statement, so point at
        // its own location instead of any of its individually-meaningless placeholders.
        var originatingStatement = region
            .Select(block => block.OriginatingStatement)
            .FirstOrDefault(statement => statement is not null);

        if (originatingStatement is not null)
        {
            return originatingStatement.SyntaxNode.GetTextLocation();
        }

        return boundFunctionMember.FunctionSymbol.FunctionDeclarationMember.GetTextLocation(boundFunctionMember.FunctionSymbol.FunctionDeclarationMember.Name.Span);
    }
}
