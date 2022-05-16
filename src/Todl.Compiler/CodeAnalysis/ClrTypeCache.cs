using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;

namespace Todl.Compiler.CodeAnalysis
{
    public sealed class ClrTypeCache
    {
        private readonly HashSet<string> loadedNamespaces = new();

        public IReadOnlySet<Assembly> Assemblies { get; private init; }
        public IReadOnlySet<ClrTypeSymbol> Types { get; private init; }
        public IReadOnlySet<string> Namespaces => loadedNamespaces;

        public readonly BuiltInTypes BuiltInTypes;

        public static readonly IReadOnlyDictionary<string, string> BuiltInTypeNames
            = new Dictionary<string, string>()
            {
                { "bool", typeof(bool).FullName },
                { "byte", typeof(byte).FullName },
                { "char", typeof(char).FullName },
                { "int", typeof(int).FullName },
                { "long", typeof(long).FullName },
                { "string", typeof(string).FullName },
                { "void", typeof(void).FullName },
                { "object", typeof(object).FullName }
            };

        private ClrTypeCache(IEnumerable<Assembly> assemblies)
        {
            Assemblies = assemblies.ToHashSet();
            Types = assemblies
                .SelectMany(a => a.GetExportedTypes())
                .Select(t => new ClrTypeSymbol() { ClrType = t })
                .ToHashSet();

            BuiltInTypes = new BuiltInTypes(this);

            PopulateNamespaces();
        }

        public ClrTypeSymbol Resolve(string name)
        {
            if (BuiltInTypeNames.ContainsKey(name))
            {
                name = BuiltInTypeNames[name];
            }

            return Types.FirstOrDefault(t => t.Name == name);
        }

        public ClrTypeCacheView CreateView(IEnumerable<ImportDirective> importDirectives) => new(this, importDirectives);

        public static ClrTypeCache FromAssemblies(IEnumerable<Assembly> assemblies)
            => new(assemblies);

        /// <summary>
        /// Populating loadedNamespaces with a full list of namespaces
        /// e.g.
        /// when input is "System.Collections.Generic"
        /// the result should be
        /// {
        ///     "System",
        ///     "System.Collections",
        ///     "System.Collections.Generic"
        /// }
        /// </summary>
        private void PopulateNamespaces()
        {
            var namespaces = Types
                .Where(t => !string.IsNullOrEmpty(t.Namespace))
                .Select(t => t.Namespace);

            foreach (var n in namespaces)
            {
                var position = -1;
                while ((position = n.IndexOf('.', position + 1)) != -1)
                {
                    loadedNamespaces.Add(n[..position]);
                }
                loadedNamespaces.Add(n);
            }
        }
    }
}
