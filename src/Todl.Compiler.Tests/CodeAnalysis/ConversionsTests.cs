using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Symbols;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class ConversionsTests
{
    private static ClrTypeSymbol Resolve(SpecialType specialType)
        => TestDefaults.DefaultClrTypeCache.ResolveSpecialType(specialType);

    [Theory]
    // from sbyte
    [InlineData(SpecialType.ClrSByte, SpecialType.ClrInt16, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrSByte, SpecialType.ClrInt32, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrSByte, SpecialType.ClrInt64, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrSByte, SpecialType.ClrFloat, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrSByte, SpecialType.ClrDouble, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrSByte, SpecialType.ClrByte, ConversionKind.None)]
    [InlineData(SpecialType.ClrSByte, SpecialType.ClrUInt32, ConversionKind.None)]
    // from byte
    [InlineData(SpecialType.ClrByte, SpecialType.ClrInt16, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrByte, SpecialType.ClrUInt16, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrByte, SpecialType.ClrInt32, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrByte, SpecialType.ClrUInt32, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrByte, SpecialType.ClrInt64, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrByte, SpecialType.ClrUInt64, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrByte, SpecialType.ClrFloat, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrByte, SpecialType.ClrDouble, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrByte, SpecialType.ClrSByte, ConversionKind.None)]
    // from short (Int16)
    [InlineData(SpecialType.ClrInt16, SpecialType.ClrInt32, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrInt16, SpecialType.ClrInt64, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrInt16, SpecialType.ClrFloat, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrInt16, SpecialType.ClrDouble, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrInt16, SpecialType.ClrUInt16, ConversionKind.None)]
    [InlineData(SpecialType.ClrInt16, SpecialType.ClrUInt32, ConversionKind.None)]
    // from ushort (UInt16)
    [InlineData(SpecialType.ClrUInt16, SpecialType.ClrInt32, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrUInt16, SpecialType.ClrUInt32, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrUInt16, SpecialType.ClrInt64, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrUInt16, SpecialType.ClrUInt64, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrUInt16, SpecialType.ClrFloat, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrUInt16, SpecialType.ClrDouble, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrUInt16, SpecialType.ClrInt16, ConversionKind.None)]
    // from int (Int32)
    [InlineData(SpecialType.ClrInt32, SpecialType.ClrInt64, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrInt32, SpecialType.ClrFloat, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrInt32, SpecialType.ClrDouble, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrInt32, SpecialType.ClrUInt32, ConversionKind.None)]
    [InlineData(SpecialType.ClrInt32, SpecialType.ClrInt16, ConversionKind.None)]
    // from uint (UInt32)
    [InlineData(SpecialType.ClrUInt32, SpecialType.ClrInt64, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrUInt32, SpecialType.ClrUInt64, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrUInt32, SpecialType.ClrFloat, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrUInt32, SpecialType.ClrDouble, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrUInt32, SpecialType.ClrInt32, ConversionKind.None)]
    // from long (Int64)
    [InlineData(SpecialType.ClrInt64, SpecialType.ClrFloat, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrInt64, SpecialType.ClrDouble, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrInt64, SpecialType.ClrUInt64, ConversionKind.None)]
    [InlineData(SpecialType.ClrInt64, SpecialType.ClrInt32, ConversionKind.None)]
    // from ulong (UInt64)
    [InlineData(SpecialType.ClrUInt64, SpecialType.ClrFloat, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrUInt64, SpecialType.ClrDouble, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrUInt64, SpecialType.ClrInt64, ConversionKind.None)]
    // from char
    [InlineData(SpecialType.ClrChar, SpecialType.ClrUInt16, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrChar, SpecialType.ClrInt32, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrChar, SpecialType.ClrUInt32, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrChar, SpecialType.ClrInt64, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrChar, SpecialType.ClrUInt64, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrChar, SpecialType.ClrFloat, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrChar, SpecialType.ClrDouble, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrChar, SpecialType.ClrInt16, ConversionKind.None)]
    [InlineData(SpecialType.ClrChar, SpecialType.ClrByte, ConversionKind.None)]
    // from float
    [InlineData(SpecialType.ClrFloat, SpecialType.ClrDouble, ConversionKind.ImplicitNumeric)]
    [InlineData(SpecialType.ClrFloat, SpecialType.ClrInt64, ConversionKind.None)]
    [InlineData(SpecialType.ClrDouble, SpecialType.ClrFloat, ConversionKind.None)]
    // bool and string never participate in numeric conversions
    [InlineData(SpecialType.ClrBoolean, SpecialType.ClrInt32, ConversionKind.None)]
    [InlineData(SpecialType.ClrInt32, SpecialType.ClrBoolean, ConversionKind.None)]
    [InlineData(SpecialType.ClrInt32, SpecialType.ClrString, ConversionKind.None)]
    public void ClassifyImplicitMatchesCSharpStandardNumericConversions(
        SpecialType source, SpecialType destination, ConversionKind expectedKind)
    {
        var sourceType = Resolve(source);
        var destinationType = Resolve(destination);

        var conversion = Conversions.ClassifyImplicit(sourceType, destinationType);

        conversion.Kind.Should().Be(expectedKind);
        conversion.Exists.Should().Be(expectedKind != ConversionKind.None);
    }

    [Theory]
    [InlineData(SpecialType.ClrInt32)]
    [InlineData(SpecialType.ClrDouble)]
    [InlineData(SpecialType.ClrString)]
    [InlineData(SpecialType.ClrBoolean)]
    public void ClassifyImplicitReturnsIdentityForTheSameType(SpecialType specialType)
    {
        var type = Resolve(specialType);

        var conversion = Conversions.ClassifyImplicit(type, type);

        conversion.Kind.Should().Be(ConversionKind.Identity);
        conversion.IsIdentity.Should().BeTrue();
        conversion.Exists.Should().BeTrue();
    }

    [Fact]
    public void ClassifyImplicitDoesNotBoxValueTypesToObject()
    {
        // boxing is out of scope for now
        var int32 = Resolve(SpecialType.ClrInt32);
        var @object = Resolve(SpecialType.ClrObject);

        Conversions.ClassifyImplicit(int32, @object).Exists.Should().BeFalse();
    }

    [Fact]
    public void ClassifyImplicitReturnsNoneForNullTypes()
    {
        var int32 = Resolve(SpecialType.ClrInt32);

        Conversions.ClassifyImplicit(null, int32).Exists.Should().BeFalse();
        Conversions.ClassifyImplicit(int32, null).Exists.Should().BeFalse();
    }
}
