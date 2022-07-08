using System.Linq;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.ControlFlowAnalysis;

internal class ControlFlowAnalyzer : BoundNodeVisitor
{
    protected override BoundMember VisitBoundFunctionMember(BoundFunctionMember boundFunctionMember)
    {
        var controlFlowGraph = ControlFlowGraph.Create(boundFunctionMember);

        AllPathsShouldReturn(controlFlowGraph, boundFunctionMember);
        AllBlocksShouldBeReachable(controlFlowGraph, boundFunctionMember.DiagnosticBuilder);

        return boundFunctionMember;
    }

    private void AllPathsShouldReturn(
        ControlFlowGraph controlFlowGraph,
        BoundFunctionMember boundFunctionMember)
    {
        var start = controlFlowGraph.StartBlock;
        var end = controlFlowGraph.EndBlock;

        var endIsReachable = end.Incoming.Any(i => i.From != start);

        if (!endIsReachable || end.Incoming.Any(i => i.From.Reachable && !i.From.IsTeminal))
        {
            boundFunctionMember.DiagnosticBuilder.Add(new Diagnostic()
            {
                Message = "Not all paths return a value",
                ErrorCode = ErrorCode.NotAllPathsReturn,
                Level = DiagnosticLevel.Error,
                TextLocation = boundFunctionMember.FunctionSymbol.FunctionDeclarationMember.Name.GetTextLocation()
            });
        }
    }

    private void AllBlocksShouldBeReachable(
        ControlFlowGraph controlFlowGraph,
        DiagnosticBag.Builder diagnosticBuilder)
    {
        var unreachableBlock = controlFlowGraph
            .Blocks
            .FirstOrDefault(block =>
                !block.Equals(controlFlowGraph.StartBlock)
                && !block.Equals(controlFlowGraph.EndBlock)
                && !block.Reachable);

        if (unreachableBlock is not null)
        {
            diagnosticBuilder.Add(new Diagnostic()
            {
                Message = "Unreachable code",
                ErrorCode = ErrorCode.UnreachableCode,
                Level = DiagnosticLevel.Warning,
                TextLocation = unreachableBlock.Statements[0].SyntaxNode.Text.GetTextLocation()
            });
        }
    }
}
