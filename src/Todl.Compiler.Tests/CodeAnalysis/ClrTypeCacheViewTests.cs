using System;
using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class ClrTypeCacheViewTests
{
    [Theory]
    [InlineData("int", typeof(int), SpecialType.ClrInt32)]
    [InlineData("System.Int32", typeof(int), SpecialType.ClrInt32)]
    [InlineData("uint", typeof(uint), SpecialType.ClrUInt32)]
    [InlineData("System.UInt32", typeof(uint), SpecialType.ClrUInt32)]
    [InlineData("long", typeof(long), SpecialType.ClrInt64)]
    [InlineData("System.Int64", typeof(long), SpecialType.ClrInt64)]
    [InlineData("ulong", typeof(ulong), SpecialType.ClrUInt64)]
    [InlineData("System.UInt64", typeof(ulong), SpecialType.ClrUInt64)]
    [InlineData("bool", typeof(bool), SpecialType.ClrBoolean)]
    [InlineData("System.Boolean", typeof(bool), SpecialType.ClrBoolean)]
    [InlineData("string", typeof(string), SpecialType.ClrString)]
    [InlineData("System.String", typeof(string), SpecialType.ClrString)]
    [InlineData("void", typeof(void), SpecialType.ClrVoid)]
    [InlineData("System.Void", typeof(void), SpecialType.ClrVoid)]
    [InlineData("object", typeof(object), SpecialType.ClrObject)]
    [InlineData("System.Object", typeof(object), SpecialType.ClrObject)]
    [InlineData("char", typeof(char), SpecialType.ClrChar)]
    [InlineData("System.Char", typeof(char), SpecialType.ClrChar)]
    [InlineData("byte", typeof(byte), SpecialType.ClrByte)]
    [InlineData("System.Byte", typeof(byte), SpecialType.ClrByte)]
    [InlineData("float", typeof(float), SpecialType.ClrFloat)]
    [InlineData("System.Single", typeof(float), SpecialType.ClrFloat)]
    [InlineData("double", typeof(double), SpecialType.ClrDouble)]
    [InlineData("System.Double", typeof(double), SpecialType.ClrDouble)]
    public void TestResolveBuiltInTypes(string typeName, Type targetType, SpecialType specialType)
    {
        var clrTypeCacheView = TestDefaults.DefaultClrTypeCache.CreateView(Enumerable.Empty<ImportDirective>());
        var resolvedType = clrTypeCacheView.ResolveBaseType(typeName);
        resolvedType.ClrType.AssemblyQualifiedName.Should().Be(targetType.AssemblyQualifiedName);
        resolvedType.SpecialType.Should().Be(specialType);
    }

    [Theory]
    [InlineData("Uri", "import { Uri } from System;", typeof(Uri))]
    [InlineData("System.Uri", "import { Uri } from System;", typeof(Uri))]
    [InlineData("Uri", "import * from System;", typeof(Uri))]
    [InlineData("System.Uri", "import * from System;", typeof(Uri))]
    [InlineData("Uri", "import { Console, Uri } from System;", typeof(Uri))]
    [InlineData("System.Uri", "import { Console, Uri } from System;", typeof(Uri))]
    public void TestResolveImportedTypes(string typeName, string importDirective, Type targetType)
    {
        var syntaxTree = SyntaxTree.Parse(SourceText.FromString(importDirective), TestDefaults.DefaultClrTypeCache, new());
        var resolvedType = syntaxTree.ClrTypeCacheView.ResolveBaseType(typeName);
        resolvedType.ClrType.AssemblyQualifiedName.Should().Be(targetType.AssemblyQualifiedName);
        resolvedType.SpecialType.Should().Be(SpecialType.None);
    }
}
