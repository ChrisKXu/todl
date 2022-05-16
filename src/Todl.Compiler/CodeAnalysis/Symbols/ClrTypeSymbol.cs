using System;

namespace Todl.Compiler.CodeAnalysis.Symbols
{
    public sealed class ClrTypeSymbol : TypeSymbol
    {
        public override bool IsNative => true;
        public override bool IsArray => ClrType.IsArray;
        public Type ClrType { get; internal init; }
        public override string Name => ClrType.FullName;
        public string Namespace => ClrType.Namespace;

        public override bool Equals(Symbol other)
        {
            if (other is ClrTypeSymbol clrTypeSymbol)
            {
                return ClrType.Equals(clrTypeSymbol.ClrType);
            }

            return false;
        }

        public override int GetHashCode()
            => ClrType.GetHashCode();

        public override string ToString() => Name;
    }
}
