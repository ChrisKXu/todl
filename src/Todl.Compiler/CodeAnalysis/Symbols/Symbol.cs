using System;
using System.Runtime.CompilerServices;

namespace Todl.Compiler.CodeAnalysis.Symbols
{
    public abstract class Symbol : IEquatable<Symbol>
    {
        public virtual bool Equals(Symbol other) => ReferenceEquals(this, other);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
        public override bool Equals(object obj) => Equals(obj as Symbol);

        public abstract string Name { get; }
    }
}
