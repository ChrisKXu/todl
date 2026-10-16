namespace Todl.Compiler.CodeAnalysis.Symbols;

public abstract class TypeSymbol : Symbol
{
    public virtual bool IsNative => false;

    public virtual bool IsArray => false;

    public abstract SpecialType SpecialType { get; }

    public abstract bool IsReferenceType { get; }

    public abstract bool IsExceptionType { get; }
}

public enum SpecialType
{
    // default, non-special value
    None,

    ClrObject,
    ClrEnum,
    ClrVoid,
    ClrBoolean,
    ClrSByte,
    ClrByte,
    ClrChar,
    ClrInt16,
    ClrUInt16,
    ClrInt32,
    ClrUInt32,
    ClrInt64,
    ClrUInt64,
    ClrFloat,
    ClrDouble,
    ClrString,
    ClrException
}
