using System.Linq;
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
        foreach (var block in controlFlowGraph.Blocks)
        {
            if (block.Equals(controlFlowGraph.StartBlock)
                || block.Equals(controlFlowGraph.EndBlock)
                || block.Reachable)
            {
                continue;
            }

            // Synthesized statements (e.g. an empty block/branch's placeholder) carry no
            // SyntaxNode; fall back to the function's own location rather than crash.
            var textLocation = block.Statements
                .Select(statement => statement.SyntaxNode)
                .FirstOrDefault(syntaxNode => syntaxNode is not null)
                ?.GetTextLocation()
                ?? boundFunctionMember.FunctionSymbol.FunctionDeclarationMember.GetTextLocation(boundFunctionMember.FunctionSymbol.FunctionDeclarationMember.Name.Span);

            diagnosticBuilder.Add(new Diagnostic()
            {
                Message = "Unreachable code",
                ErrorCode = ErrorCode.UnreachableCode,
                Level = DiagnosticLevel.Warning,
                TextLocation = textLocation
            });
        }
    }
}
