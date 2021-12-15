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

    protected override BoundMember VisitFunctionMember(BoundFunctionMember boundFunctionMember)
    {
        var controlFlowGraph = ControlFlowGraph.Create(boundFunctionMember);

        if (!boundFunctionMember.ReturnType.Equals(TypeSymbol.ClrVoid))
        {
            AllPathShouldReturn(controlFlowGraph, boundFunctionMember);
        }

        return boundFunctionMember;
    }

    private void AllPathShouldReturn(
        ControlFlowGraph controlFlowGraph,
        BoundFunctionMember boundFunctionMember)
    {
        var end = controlFlowGraph.EndBlock;

        if (end.Incoming.Any(i => !i.From.IsTeminal))
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
}
