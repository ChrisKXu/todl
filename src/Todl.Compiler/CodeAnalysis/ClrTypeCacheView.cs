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

        if (typeAliases.ContainsKey(name))
        {
            return typeAliases[name];
        }

        return clrTypeCache.Resolve(name);
    }

    public ClrTypeSymbol ResolveType(NameExpression nameExpression)
        => ResolveBaseType(nameExpression.Text.ToString());

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

        var importedTypes = importDirectives.SelectMany(importDirective =>
        {
            var importedNamespace = importDirective.Namespace.ToString();
            var types = clrTypeCache
                .Types
                .Where(t => importedNamespace.Equals(t.Namespace));

            if (!importDirective.ImportAll)
            {
                var importedNames = importDirective
                    .ImportedNames
                    .Select(n => $"{importedNamespace}.{n}")
                    .ToHashSet();

                types = types.Where(t => importedNames.Contains(t.Name));
            }

            return types;
        }).Distinct();

        return importedTypes.ToImmutableDictionary(t => t.ClrType.Name);
    }

    internal ClrTypeCacheView(ClrTypeCache cache, IEnumerable<ImportDirective> importDirectives)
    {
        clrTypeCache = cache;
        typeAliases = ImportTypeAliases(importDirectives);
    }
}
