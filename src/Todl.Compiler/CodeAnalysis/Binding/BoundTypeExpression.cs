using System;
using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundTypeExpression : BoundExpression
    {
        public override TypeSymbol ResultType => ClrTypeSymbol.MapClrType<Type>();
        public TypeSymbol BoundType { get; internal init; }
    }
}
