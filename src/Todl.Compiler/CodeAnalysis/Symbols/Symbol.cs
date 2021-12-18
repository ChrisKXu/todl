using System;

namespace Todl.Compiler.CodeAnalysis.Symbols
{
    public abstract class Symbol : IEquatable<Symbol>
    {
        public abstract bool Equals(Symbol other);
        public abstract override int GetHashCode();
        public override bool Equals(object obj) => Equals(obj as Symbol);

        public abstract string Name { get; }
    }
}
