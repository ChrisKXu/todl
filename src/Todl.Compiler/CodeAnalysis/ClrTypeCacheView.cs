using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis;

public sealed class ClrTypeCacheView
{
    private readonly ClrTypeCache clrTypeCache;
    private readonly ImmutableDictionary<string, ClrTypeSymbol> typeAliases;

    internal ClrTypeSymbol ResolveBaseType(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (typeAliases.TryGetValue(name, out var aliased))
        {
            return aliased;
        }

        return clrTypeCache.Resolve(name);
    }

    public ClrTypeSymbol ResolveType(NameExpression nameExpression)
        => ResolveBaseType(nameExpression.CanonicalName);

    public ClrTypeSymbol ResolveType(TypeExpression typeExpression)
    {
        var type = ResolveType(typeExpression.BaseTypeExpression);

        // An invalid '?' is reported by the binder; keep the base type so symbols stay resolvable.
        if (typeExpression.IsNullableType)
        {
            type = clrTypeCache.ResolveNullable(type) ?? type;
        }

        return typeExpression.IsArrayType
            ? ResolveArrayType(type, typeExpression.ArrayRankSpecifiers.Length)
            : type;
    }

    internal ClrTypeSymbol ResolveArrayType(ClrTypeSymbol elementType, int rank)
    {
        if (elementType is null)
        {
            return null;
        }

        var clrType = elementType.ClrType;
        for (var i = 0; i < rank; ++i)
        {
            clrType = clrType.MakeArrayType();
        }

        return new(clrType, isExceptionType: false);
    }

    private ImmutableDictionary<string, ClrTypeSymbol> ImportTypeAliases(IEnumerable<ImportDirective> importDirectives)
    {
        if (importDirectives == null)
        {
            throw new ArgumentNullException(nameof(importDirectives));
        }

        var builder = ImmutableDictionary.CreateBuilder<string, ClrTypeSymbol>();

        foreach (var importDirective in importDirectives)
        {
            var ns = importDirective.Namespace;

            if (importDirective.ImportAll)
            {
                // Use GetTypesInNamespace for wildcard imports (lazy, cached)
                foreach (var symbol in clrTypeCache.GetTypesInNamespace(ns))
                {
                    builder.TryAdd(symbol.ClrType.Name, symbol);
                }
            }
            else
            {
                // Import specific types - direct lookup
                foreach (var typeName in importDirective.ImportedNames)
                {
                    var fullName = $"{ns}.{typeName}";
                    var symbol = clrTypeCache.Resolve(fullName);
                    if (symbol != null)
                    {
                        builder.TryAdd(typeName, symbol);
                    }
                }
            }
        }

        return builder.ToImmutable();
    }

    internal ClrTypeCacheView(ClrTypeCache cache, IEnumerable<ImportDirective> importDirectives)
    {
        clrTypeCache = cache;
        typeAliases = ImportTypeAliases(importDirectives);
    }
}
