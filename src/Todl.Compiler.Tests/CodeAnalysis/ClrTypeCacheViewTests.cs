using System;
using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class ClrTypeCacheViewTests
{
    [Theory]
    [InlineData("int", typeof(int))]
    [InlineData("System.Int32", typeof(int))]
    [InlineData("long", typeof(long))]
    [InlineData("System.Int64", typeof(long))]
    [InlineData("bool", typeof(bool))]
    [InlineData("System.Boolean", typeof(bool))]
    [InlineData("string", typeof(string))]
    [InlineData("System.String", typeof(string))]
    [InlineData("void", typeof(void))]
    [InlineData("System.Void", typeof(void))]
    [InlineData("object", typeof(object))]
    [InlineData("System.Object", typeof(object))]
    [InlineData("char", typeof(char))]
    [InlineData("System.Char", typeof(char))]
    [InlineData("byte", typeof(byte))]
    [InlineData("System.Byte", typeof(byte))]
    public void TestResolveBuiltInTypes(string typeName, Type targetType)
    {
        var clrTypeCacheView = TestDefaults.DefaultClrTypeCache.CreateView(Enumerable.Empty<ImportDirective>());
        var resolvedType = clrTypeCacheView.ResolveBaseType(typeName);
        resolvedType.ClrType.AssemblyQualifiedName.Should().Be(targetType.AssemblyQualifiedName);
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
        var syntaxTree = SyntaxTree.Parse(SourceText.FromString(importDirective), TestDefaults.DefaultClrTypeCache);
        var resolvedType = syntaxTree.ClrTypeCacheView.ResolveBaseType(typeName);
        resolvedType.ClrType.AssemblyQualifiedName.Should().Be(targetType.AssemblyQualifiedName);
    }
}
