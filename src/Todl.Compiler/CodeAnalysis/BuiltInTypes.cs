using Todl.Compiler.CodeAnalysis.Symbols;

namespace Todl.Compiler.CodeAnalysis;

public sealed class BuiltInTypes
{
    public readonly ClrTypeSymbol Boolean;
    public readonly ClrTypeSymbol Int32;
    public readonly ClrTypeSymbol Void;
    public readonly ClrTypeSymbol Object;
    public readonly ClrTypeSymbol String;

    internal BuiltInTypes(ClrTypeCache clrTypeCache)
    {
        Boolean = clrTypeCache.Resolve(typeof(bool).FullName);
        Int32 = clrTypeCache.Resolve(typeof(int).FullName);
        Void = clrTypeCache.Resolve(typeof(void).FullName);
        Object = clrTypeCache.Resolve(typeof(object).FullName);
        String = clrTypeCache.Resolve(typeof(string).FullName);
    }
}
