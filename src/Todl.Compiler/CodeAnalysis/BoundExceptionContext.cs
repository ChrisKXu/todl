namespace Todl.Compiler.CodeAnalysis;

/// <summary>
/// Tracks the exception-handling state that constrains jump statements within a function:
/// whether the code is inside an except block (for rethrow) and inside a finally block
/// (for return, break and continue). It is function-scoped: a binder that starts a new
/// function-like body supplies a fresh <see cref="Empty"/> context.
/// </summary>
public sealed class BoundExceptionContext
{
    public static readonly BoundExceptionContext Empty = new();

    /// <summary>True when a rethrow is permitted: inside an except block, and not in a finally block nested in it.</summary>
    public bool InExceptBlock { get; private init; }

    /// <summary>True when inside the innermost enclosing finally block.</summary>
    public bool InFinally { get; private init; }

    /// <summary>The loop context that was in effect where the innermost finally block begins.</summary>
    public BoundLoopContext FinallyLoopContext { get; private init; }

    public BoundExceptionContext EnterExceptBlock()
        => new()
        {
            InExceptBlock = true,
            InFinally = InFinally,
            FinallyLoopContext = FinallyLoopContext
        };

    public BoundExceptionContext EnterFinallyBlock(BoundLoopContext currentLoopContext)
        => new()
        {
            InExceptBlock = false,
            InFinally = true,
            FinallyLoopContext = currentLoopContext
        };

    /// <summary>
    /// Whether a break/continue targeting <paramref name="target"/> would exit the innermost finally block,
    /// i.e. the target loop is not nested inside that block.
    /// </summary>
    public bool ExitsFinally(BoundLoopContext target)
    {
        if (!InFinally)
        {
            return false;
        }

        for (var context = FinallyLoopContext; context is not null; context = context.Parent)
        {
            if (ReferenceEquals(context, target))
            {
                return true;
            }
        }

        return false;
    }
}
