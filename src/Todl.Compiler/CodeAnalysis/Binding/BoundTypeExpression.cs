using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding;

public sealed class BoundTypeExpression : BoundExpression
{
    internal TypeSymbol TargetType { get; init; }

    public override TypeSymbol ResultType => TargetType;
}
