namespace Todl.Compiler.CodeAnalysis.Text;

public readonly record struct LinePositionSpan(LinePosition Start, LinePosition End)
{
    public bool IsValid => Start.Line < End.Line || (Start.Line == End.Line && Start.Character <= End.Character);
}
