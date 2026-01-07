using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        var baseType = ResolveType(typeExpression.BaseTypeExpression);
        if (!typeExpression.IsArrayType)
        {
            return baseType;
        }

        if (baseType is null)
        {
            return null;
        }

        var resolvedTypeString = typeExpression.Text.ToString().Replace(typeExpression.BaseTypeExpression.Text.ToString(), baseType.Name);

        return new(baseType.ClrType.Assembly.GetType(resolvedTypeString));
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
