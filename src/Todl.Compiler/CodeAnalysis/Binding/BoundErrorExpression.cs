using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    // This class acts as a placeholder for a binding error
    public sealed class BoundErrorExpression : BoundExpression
    {
        public override TypeSymbol ResultType => TypeSymbol.ClrObject;
    }
}
