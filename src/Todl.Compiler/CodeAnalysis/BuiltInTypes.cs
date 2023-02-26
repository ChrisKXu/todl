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
        Boolean = clrTypeCache.ResolveSpecialType(SpecialType.ClrBoolean);
        Byte = clrTypeCache.ResolveSpecialType(SpecialType.ClrByte);
        Char = clrTypeCache.ResolveSpecialType(SpecialType.ClrChar);
        Int32 = clrTypeCache.ResolveSpecialType(SpecialType.ClrInt32);
        UInt32 = clrTypeCache.ResolveSpecialType(SpecialType.ClrUInt32);
        Int64 = clrTypeCache.ResolveSpecialType(SpecialType.ClrInt64);
        UInt64 = clrTypeCache.ResolveSpecialType(SpecialType.ClrUInt64);
        Float = clrTypeCache.ResolveSpecialType(SpecialType.ClrFloat);
        Double = clrTypeCache.ResolveSpecialType(SpecialType.ClrDouble);
        Void = clrTypeCache.ResolveSpecialType(SpecialType.ClrVoid);
        Object = clrTypeCache.ResolveSpecialType(SpecialType.ClrObject);
        String = clrTypeCache.ResolveSpecialType(SpecialType.ClrString);
    }
}
