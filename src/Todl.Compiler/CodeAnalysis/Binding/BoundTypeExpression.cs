using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundTypeExpression : BoundExpression
    {
        public TypeSymbol TargetType { get; internal init; }

        public override TypeSymbol ResultType => TargetType;
    }
}
