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
}
