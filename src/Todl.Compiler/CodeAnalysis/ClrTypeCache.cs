using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Todl.Compiler.CodeAnalysis
{
    public class ClrTypeCache
    {
        private readonly HashSet<string> loadedNamespaces = new();

        public IReadOnlySet<Assembly> Assemblies { get; private init; }
        public IReadOnlySet<Type> Types { get; private init; }
        public IReadOnlySet<string> Namespaces => loadedNamespaces;

        private ClrTypeCache(IEnumerable<Assembly> assemblies)
        {
            Assemblies = assemblies.ToHashSet();
            Types = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(a => a.IsPublic)
                .ToHashSet();

            PopulateNamespaces();
        }

        public static ClrTypeCache FromType(Type type)
        {
            var assemblies = new HashSet<Assembly>();

            var stack = new Stack<Assembly>();
            stack.Push(type.Assembly);

            while (stack.Any())
            {
                var current = stack.Pop();
                if (!assemblies.Contains(current))
                {
                    assemblies.Add(current);

                    foreach (var reference in current.GetReferencedAssemblies())
                    {
                        stack.Push(Assembly.Load(reference));
                    }
                }
            }

            return new ClrTypeCache(assemblies);
        }

        public static ClrTypeCache FromAssemblies(IEnumerable<Assembly> assemblies)
            => new ClrTypeCache(assemblies);

        public static ClrTypeCache Default => ClrTypeCache.FromType(typeof(ClrTypeCache));

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
                    loadedNamespaces.Add(n.Substring(0, position));
                }
                loadedNamespaces.Add(n);
            }
        }
    }
}
