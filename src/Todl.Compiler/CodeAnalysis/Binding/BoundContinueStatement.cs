using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundContinueStatement : BoundStatement
{
}

public partial class Binder
{
    private BoundContinueStatement BindContinueStatement(ContinueStatement continueStatement)
        => BoundNodeFactory.CreateBoundContinueStatement(continueStatement);
}
