using System;
using FluentAssertions;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class ClrTypeSymbolTests
{
    [Theory]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(object), true)]
    [InlineData(typeof(Exception), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(bool), false)]
    [InlineData(typeof(double), false)]
    [InlineData(typeof(DateTime), false)]
    public void IsReferenceTypeShouldMatchClrValueTypeSemantics(Type clrType, bool expectedIsReferenceType)
    {
        var symbol = TestDefaults.DefaultClrTypeCache.Resolve(clrType.FullName);

        symbol.IsReferenceType.Should().Be(expectedIsReferenceType);
    }

    [Theory]
    [InlineData(typeof(FormatException), typeof(Exception), true)]
    [InlineData(typeof(FormatException), typeof(SystemException), true)]
    [InlineData(typeof(FormatException), typeof(object), true)]
    [InlineData(typeof(Exception), typeof(FormatException), false)]
    [InlineData(typeof(Exception), typeof(Exception), false)]
    [InlineData(typeof(string), typeof(Exception), false)]
    [InlineData(typeof(int), typeof(Exception), false)]
    public void DerivesFromShouldFollowTheBaseClassChain(Type derived, Type baseType, bool expected)
    {
        var derivedSymbol = TestDefaults.DefaultClrTypeCache.Resolve(derived.FullName);
        var baseSymbol = TestDefaults.DefaultClrTypeCache.Resolve(baseType.FullName);

        derivedSymbol.DerivesFrom(baseSymbol).Should().Be(expected);
    }

    [Theory]
    [InlineData(typeof(Exception), true)]
    [InlineData(typeof(InvalidOperationException), true)]
    [InlineData(typeof(System.IO.IOException), true)]
    [InlineData(typeof(object), false)]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(Uri), false)]
    public void IsExceptionTypeShouldAcceptExceptionAndItsDerivedTypes(Type type, bool expected)
    {
        var symbol = TestDefaults.DefaultClrTypeCache.Resolve(type.FullName);

        symbol.IsExceptionType(TestDefaults.DefaultClrTypeCache.BuiltInTypes).Should().Be(expected);
    }
}
