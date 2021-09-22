using System;
using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    public sealed class BoundImportDirective
    {
        public IReadOnlyDictionary<string, Type> ImportedTypes { get; internal init; }
        public string Namespace { get; internal init; }
    }

    public sealed partial class Binder
    {
        internal BoundImportDirective BindImportDirective(ImportDirective importDirective)
        {
            var availableTypes = clrTypeCache.Types
                .Where(t => t.IsPublic && t.Namespace == importDirective.Namespace);

            if (importDirective.ImportedNames.Any())
            {
                availableTypes = availableTypes.Where(t => importDirective.ImportedNames.Contains(t.Name));
            }

            return new BoundImportDirective()
            {
                Namespace = importDirective.Namespace,
                ImportedTypes = availableTypes.ToDictionary(
                    t => t.Name,
                    t => t)
            };
        }
    }
}
