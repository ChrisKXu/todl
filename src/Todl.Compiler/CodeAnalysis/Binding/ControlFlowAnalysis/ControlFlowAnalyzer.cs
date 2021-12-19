using System.Linq;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.ControlFlowAnalysis;

internal class ControlFlowAnalyzer : BoundNodeVisitor
{
    private readonly DiagnosticBag.Builder diagnosticBuilder;

    public ControlFlowAnalyzer(DiagnosticBag.Builder diagnosticBuilder)
    {
        this.diagnosticBuilder = diagnosticBuilder;
    }

    protected override BoundMember VisitBoundFunctionMember(BoundFunctionMember boundFunctionMember)
    {
        var controlFlowGraph = ControlFlowGraph.Create(boundFunctionMember);

        if (!boundFunctionMember.ReturnType.Equals(TypeSymbol.ClrVoid))
        {
            AllPathShouldReturn(controlFlowGraph, boundFunctionMember);
        }

        AllBlocksShouldBeReachable(controlFlowGraph);

        return boundFunctionMember;
    }

    private void AllPathShouldReturn(
        ControlFlowGraph controlFlowGraph,
        BoundFunctionMember boundFunctionMember)
    {
        var end = controlFlowGraph.EndBlock;

        if (!end.Reachable || end.Incoming.Any(i => !i.From.IsTeminal))
        {
            diagnosticBuilder.Add(new Diagnostic()
            {
                Message = "Not all paths return a value",
                ErrorCode = ErrorCode.NotAllPathsReturn,
                Level = DiagnosticLevel.Error,
                TextLocation = boundFunctionMember.FunctionSymbol.FunctionDeclarationMember.Name.GetTextLocation()
            });
        }
    }

    private void AllBlocksShouldBeReachable(
        ControlFlowGraph controlFlowGraph)
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
