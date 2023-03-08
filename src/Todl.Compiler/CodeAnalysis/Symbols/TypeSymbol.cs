namespace Todl.Compiler.CodeAnalysis.Symbols;

/// <summary>
/// Todl supports both types defined in CLR and types defined by the user program.
/// </summary>
public abstract class TypeSymbol : Symbol
{
    /// <summary>
    /// Gets if a type is a native CLR type
    /// </summary>
    public virtual bool IsNative => false;

    public virtual bool IsArray => false;

    public abstract SpecialType SpecialType { get; }
}

public enum SpecialType
{
    // default, non-special value
    None,

    ClrObject,
    ClrEnum,
    ClrVoid,
    ClrBoolean,
    ClrByte,
    ClrChar,
    ClrInt32,
    ClrUInt32,
    ClrInt64,
    ClrUInt64,
    ClrFloat,
    ClrDouble,
    ClrString
}
