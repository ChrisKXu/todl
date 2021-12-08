using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundTypeExpression : BoundExpression
    {
        internal TypeSymbol TargetType { get; init; }

        public override TypeSymbol ResultType => TargetType;
    }
}
