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

        private static readonly Dictionary<string, string> builtInTypes = new()
        {
            { "bool", typeof(bool).FullName },
            { "byte", typeof(byte).FullName },
            { "char", typeof(char).FullName },
            { "int", typeof(int).FullName },
            { "long", typeof(long).FullName },
            { "string", typeof(string).FullName },
            { "void", typeof(void).FullName }
        };

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

            if (builtInTypes.ContainsKey(name))
            {
                name = builtInTypes[name];
            }

            return clrTypeCache.Resolve(name);
        }

        public ClrTypeSymbol ResolveType(NameExpression nameExpression)
            => ResolveBaseType(nameExpression.Text.ToString());

        public ClrTypeSymbol ResolveType(TypeExpression typeExpression)
        {
            if (!typeExpression.IsArrayType)
            {
                return ResolveBaseType(typeExpression.Text.ToString());
            }

            throw new NotImplementedException();
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
