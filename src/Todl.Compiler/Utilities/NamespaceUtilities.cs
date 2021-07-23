using System.Collections.Generic;

namespace Todl.Compiler.Utilities
{
    internal static class NamespaceUtilities
    {
        // This method gets full list of namespaces, e.g.
        // when input is "System.Collections.Generic"
        // the result should be
        // {
        //     "System",
        //     "System.Collections",
        //     "System.Collections.Generic"
        // }
        public static IReadOnlySet<string> GetFullNamespaces(IEnumerable<string> namespaces)
        {
            var hashSet = new HashSet<string>();

            foreach (var n in namespaces)
            {
                var position = -1;
                while ((position = n.IndexOf('.', position + 1)) != -1)
                {
                    hashSet.Add(n.Substring(0, position));
                }
                hashSet.Add(n);
            }

            return hashSet;
        }
    }
}
