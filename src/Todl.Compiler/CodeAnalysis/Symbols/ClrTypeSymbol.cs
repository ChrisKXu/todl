using System;

namespace Todl.Compiler.CodeAnalysis.Symbols
{
    public sealed class ClrTypeSymbol : TypeSymbol
    {
        public override bool IsNative => true;
        public Type ClrType { get; private init; }
        public override string Name => ClrType.FullName;

        public override bool Equals(Symbol other)
        {
            if (other is ClrTypeSymbol clrTypeSymbol)
            {
                return ClrType.Equals(clrTypeSymbol.ClrType);
            }

            return false;
        }

        public static TypeSymbol MapClrType<T>()
            => MapClrType(typeof(T));

        public static TypeSymbol MapClrType(Type type)
            => new ClrTypeSymbol() { ClrType = type };

        public override int GetHashCode()
            => ClrType.GetHashCode();

        public override string ToString() => Name;
    }
}
