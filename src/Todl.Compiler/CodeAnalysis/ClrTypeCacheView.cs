using System;
using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis
{
    public sealed class ClrTypeCacheView
    {
        private readonly ClrTypeCache clrTypeCache;
        private readonly IDictionary<string, ClrTypeSymbol> typeAliases;

        private ClrTypeSymbol ResolveBaseType(string name)
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

            return new()
            {
                ClrType = baseType.ClrType.Assembly.GetType(resolvedTypeString)
            };
        }

        private IDictionary<string, ClrTypeSymbol> ImportTypeAliases(IEnumerable<ImportDirective> importDirectives)
        {
            if (importDirectives == null)
            {
                throw new ArgumentNullException(nameof(importDirectives));
            }

            var importedTypes = importDirectives.SelectMany(importDirective =>
            {
                var types = clrTypeCache
                    .Types
                    .Where(t => importDirective.Namespace.Equals(t.Namespace));
                if (!importDirective.ImportAll)
                {
                    types = types.Where(t => importDirective.ImportedNames.Contains(t.Name));
                }

                return types;
            }).Distinct();

            return importedTypes.ToDictionary(t => t.Name);
        }

        internal ClrTypeCacheView(ClrTypeCache cache, IEnumerable<ImportDirective> importDirectives)
        {
            clrTypeCache = cache;
            typeAliases = ImportTypeAliases(importDirectives);
        }
    }
}
