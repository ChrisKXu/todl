using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis;

public sealed class BuiltInTypes
{
    public readonly ClrTypeSymbol Boolean;
    public readonly ClrTypeSymbol Byte;
    public readonly ClrTypeSymbol Char;
    public readonly ClrTypeSymbol Int32;
    public readonly ClrTypeSymbol UInt32;
    public readonly ClrTypeSymbol Int64;
    public readonly ClrTypeSymbol UInt64;
    public readonly ClrTypeSymbol Float;
    public readonly ClrTypeSymbol Double;
    public readonly ClrTypeSymbol Void;
    public readonly ClrTypeSymbol Object;
    public readonly ClrTypeSymbol String;

    internal BuiltInTypes(ClrTypeCache clrTypeCache)
    {
        Boolean = clrTypeCache.Resolve(typeof(bool).FullName);
        Byte = clrTypeCache.Resolve(typeof(byte).FullName);
        Char = clrTypeCache.Resolve(typeof(char).FullName);
        Int32 = clrTypeCache.Resolve(typeof(int).FullName);
        UInt32 = clrTypeCache.Resolve(typeof(uint).FullName);
        Int64 = clrTypeCache.Resolve(typeof(long).FullName);
        UInt64 = clrTypeCache.Resolve(typeof(ulong).FullName);
        Float = clrTypeCache.Resolve(typeof(float).FullName);
        Double = clrTypeCache.Resolve(typeof(double).FullName);
        Void = clrTypeCache.Resolve(typeof(void).FullName);
        Object = clrTypeCache.Resolve(typeof(object).FullName);
        String = clrTypeCache.Resolve(typeof(string).FullName);
    }
}
