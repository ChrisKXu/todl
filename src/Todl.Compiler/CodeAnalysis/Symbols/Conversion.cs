using System.Collections.Generic;
using System.Collections.Immutable;

namespace Todl.Compiler.CodeAnalysis.Symbols;

public enum ConversionKind
{
    None,
    Identity,
    ImplicitNumeric
}

public readonly struct Conversion
{
    public ConversionKind Kind { get; }
    public bool Exists => Kind != ConversionKind.None;
    public bool IsIdentity => Kind == ConversionKind.Identity;
    public bool IsImplicitNumeric => Kind == ConversionKind.ImplicitNumeric;

    private Conversion(ConversionKind kind)
    {
        Kind = kind;
    }

    public static readonly Conversion None = new(ConversionKind.None);
    public static readonly Conversion Identity = new(ConversionKind.Identity);
    public static readonly Conversion ImplicitNumeric = new(ConversionKind.ImplicitNumeric);
}

// Mirrors C#'s standard implicit numeric conversions (ECMA-334 §10.2.1), minus decimal.
// Boxing, reference, and user-defined conversions are out of scope for now.
public static class Conversions
{
    private static readonly ImmutableHashSet<(SpecialType Source, SpecialType Destination)> implicitNumericConversions =
        new HashSet<(SpecialType, SpecialType)>
        {
            // from sbyte
            (SpecialType.ClrSByte, SpecialType.ClrInt16),
            (SpecialType.ClrSByte, SpecialType.ClrInt32),
            (SpecialType.ClrSByte, SpecialType.ClrInt64),
            (SpecialType.ClrSByte, SpecialType.ClrFloat),
            (SpecialType.ClrSByte, SpecialType.ClrDouble),

            // from byte
            (SpecialType.ClrByte, SpecialType.ClrInt16),
            (SpecialType.ClrByte, SpecialType.ClrUInt16),
            (SpecialType.ClrByte, SpecialType.ClrInt32),
            (SpecialType.ClrByte, SpecialType.ClrUInt32),
            (SpecialType.ClrByte, SpecialType.ClrInt64),
            (SpecialType.ClrByte, SpecialType.ClrUInt64),
            (SpecialType.ClrByte, SpecialType.ClrFloat),
            (SpecialType.ClrByte, SpecialType.ClrDouble),

            // from short (Int16)
            (SpecialType.ClrInt16, SpecialType.ClrInt32),
            (SpecialType.ClrInt16, SpecialType.ClrInt64),
            (SpecialType.ClrInt16, SpecialType.ClrFloat),
            (SpecialType.ClrInt16, SpecialType.ClrDouble),

            // from ushort (UInt16)
            (SpecialType.ClrUInt16, SpecialType.ClrInt32),
            (SpecialType.ClrUInt16, SpecialType.ClrUInt32),
            (SpecialType.ClrUInt16, SpecialType.ClrInt64),
            (SpecialType.ClrUInt16, SpecialType.ClrUInt64),
            (SpecialType.ClrUInt16, SpecialType.ClrFloat),
            (SpecialType.ClrUInt16, SpecialType.ClrDouble),

            // from int (Int32)
            (SpecialType.ClrInt32, SpecialType.ClrInt64),
            (SpecialType.ClrInt32, SpecialType.ClrFloat),
            (SpecialType.ClrInt32, SpecialType.ClrDouble),

            // from uint (UInt32)
            (SpecialType.ClrUInt32, SpecialType.ClrInt64),
            (SpecialType.ClrUInt32, SpecialType.ClrUInt64),
            (SpecialType.ClrUInt32, SpecialType.ClrFloat),
            (SpecialType.ClrUInt32, SpecialType.ClrDouble),

            // from long (Int64)
            (SpecialType.ClrInt64, SpecialType.ClrFloat),
            (SpecialType.ClrInt64, SpecialType.ClrDouble),

            // from ulong (UInt64)
            (SpecialType.ClrUInt64, SpecialType.ClrFloat),
            (SpecialType.ClrUInt64, SpecialType.ClrDouble),

            // from char
            (SpecialType.ClrChar, SpecialType.ClrUInt16),
            (SpecialType.ClrChar, SpecialType.ClrInt32),
            (SpecialType.ClrChar, SpecialType.ClrUInt32),
            (SpecialType.ClrChar, SpecialType.ClrInt64),
            (SpecialType.ClrChar, SpecialType.ClrUInt64),
            (SpecialType.ClrChar, SpecialType.ClrFloat),
            (SpecialType.ClrChar, SpecialType.ClrDouble),

            // from float
            (SpecialType.ClrFloat, SpecialType.ClrDouble),
        }.ToImmutableHashSet();

    public static Conversion ClassifyImplicit(TypeSymbol source, TypeSymbol destination)
    {
        if (source is null || destination is null)
        {
            return Conversion.None;
        }

        if (source.Equals(destination))
        {
            return Conversion.Identity;
        }

        return implicitNumericConversions.Contains((source.SpecialType, destination.SpecialType))
            ? Conversion.ImplicitNumeric
            : Conversion.None;
    }
}
