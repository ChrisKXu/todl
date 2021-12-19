using System;

namespace Todl.Compiler.CodeAnalysis.Symbols
{
    public abstract class VariableSymbol : Symbol
    {
        public abstract bool ReadOnly { get; }
        public abstract TypeSymbol Type { get; }
    }
}
