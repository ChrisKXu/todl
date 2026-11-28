using System.Linq;
using FluentAssertions;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class ClrTypeCacheTests
{
    [Fact]
    public void ResolveByNameShouldAgreeWithWildcardImportOnTypeVisibility()
    {
        var coreAssembly = TestDefaults.MetadataLoadContext.CoreAssembly;
        var nonPublicType = coreAssembly
            .GetTypes()
            .First(t => !t.IsPublic && !t.IsNested && !t.IsGenericType && t.Namespace != null);

        // Explicit-name resolution must not surface a type that wildcard-import
        // resolution (GetTypesInNamespace, which uses GetExportedTypes) would hide.
        TestDefaults.DefaultClrTypeCache.Resolve(nonPublicType.FullName).Should().BeNull();

        TestDefaults.DefaultClrTypeCache
            .GetTypesInNamespace(nonPublicType.Namespace)
            .Should().NotContain(symbol => symbol.ClrType.FullName == nonPublicType.FullName);
    }

    [Fact]
    public void ResolveByNameShouldFindPublicTypes()
    {
        var resolved = TestDefaults.DefaultClrTypeCache.Resolve(typeof(System.Exception).FullName);

        resolved.Should().NotBeNull();
        resolved.ClrType.FullName.Should().Be(typeof(System.Exception).FullName);
    }

    [Fact]
    public void ResolveByTypeShouldSupportClosedConstructedGenericTypes()
    {
        var openListType = TestDefaults.MetadataLoadContext.GetAssemblies()
            .Select(a => a.GetType("System.Collections.Generic.List`1"))
            .First(t => t != null);
        var closedListType = openListType.MakeGenericType(TestDefaults.DefaultClrTypeCache.BuiltInTypes.Int32.ClrType);

        var resolved = TestDefaults.DefaultClrTypeCache.Resolve(closedListType);

        resolved.Should().NotBeNull();
        resolved.ClrType.Should().Be(closedListType);
    }

    [Fact]
    public void ResolveByTypeShouldRejectOpenGenericTypesAndGenericParameters()
    {
        var openListType = TestDefaults.MetadataLoadContext.GetAssemblies()
            .Select(a => a.GetType("System.Collections.Generic.List`1"))
            .First(t => t != null);

        TestDefaults.DefaultClrTypeCache.Resolve(openListType).Should().BeNull();
        TestDefaults.DefaultClrTypeCache.Resolve(openListType.GetGenericArguments()[0]).Should().BeNull();
    }

    [Fact]
    public void ResolveNullableShouldReturnTheSameSymbolAsResolvingTheClosedClrType()
    {
        var cache = TestDefaults.DefaultClrTypeCache;
        var int32 = cache.BuiltInTypes.Int32;
        var closedNullable = cache.CoreAssembly.GetType("System.Nullable`1").MakeGenericType(int32.ClrType);

        var nullable = cache.ResolveNullable(int32);

        nullable.Should().BeSameAs(cache.Resolve(closedNullable));
        nullable.IsNullableValueType.Should().BeTrue();
        nullable.UnderlyingNullableType.Should().Be(int32);
        cache.ResolveNullable(nullable).Should().BeNull();
    }

    [Fact]
    public void ResolveNullableShouldRejectVoidAndReferenceTypes()
    {
        var cache = TestDefaults.DefaultClrTypeCache;

        cache.ResolveNullable(cache.BuiltInTypes.Void).Should().BeNull();
        cache.ResolveNullable(cache.BuiltInTypes.String).Should().BeNull();
        cache.ResolveNullable(cache.BuiltInTypes.Object).Should().BeNull();
        cache.ResolveNullable(null).Should().BeNull();
    }
}
