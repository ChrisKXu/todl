using System;
using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis
{
    public sealed class ClrTypeCacheView
    {
        private readonly ClrTypeCache clrTypeCache;
        private readonly IDictionary<string, Type> typeAliases;

        public Type ResolveType(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (typeAliases.ContainsKey(name))
            {
                return typeAliases[name];
            }

            return clrTypeCache.Types.FirstOrDefault(t => t.FullName == name);
        }

        private IDictionary<string, Type> ImportTypeAliases(IEnumerable<ImportDirective> importDirectives)
        {
            if (importDirectives == null)
            {
                throw new ArgumentNullException(nameof(importDirectives));
            }

            var importedTypes = importDirectives.SelectMany(importDirective =>
            {
                var types = clrTypeCache.Types.Where(t => t.Namespace == importDirective.Namespace);
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
            this.clrTypeCache = cache;
            typeAliases = ImportTypeAliases(importDirectives);
        }
    }
}
