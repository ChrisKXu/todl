using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundBreakStatement : BoundStatement
{
}

public partial class Binder
{
    private BoundBreakStatement BindBreakStatement(BreakStatement breakStatement)
        => BoundNodeFactory.CreateBoundBreakStatement(breakStatement);
}
