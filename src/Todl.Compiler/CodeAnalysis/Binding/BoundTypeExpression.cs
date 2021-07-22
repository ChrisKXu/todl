using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundTypeExpression : BoundExpression
    {
        public override TypeSymbol ResultType { get; internal init; }
    }
}
