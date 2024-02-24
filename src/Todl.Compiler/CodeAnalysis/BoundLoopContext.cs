namespace Todl.Compiler.CodeAnalysis;

/// <summary>
/// The BoundLoopContext is used to help identify information related to a loop structure, such as nested loops
/// </summary>
public sealed class BoundLoopContext
{
    public BoundLoopContext Parent { get; internal init; }

    public BoundLoopContext CreateChildContext()
    {
        return new BoundLoopContext()
        {
            Parent = this
        };
    }
}
