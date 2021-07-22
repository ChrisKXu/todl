using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Todl.Compiler.Diagnostics;
using Todl.Compiler.Utilities;

namespace Todl.Compiler.CodeAnalysis.Binding
{
    /// <summary>
    /// Binder reorganizes SyntaxTree elements into Bound counterparts
    /// and prepares necessary information for Emitter to use in the emit process
    /// </summary>
    public sealed partial class Binder
    {
        private readonly List<Diagnostic> diagnostics = new();
        private readonly BinderFlags binderFlags;
        private readonly IReadOnlyDictionary<string, Type> loadedTypes;
        private readonly IReadOnlySet<string> loadedNamespaces;
        private readonly List<Assembly> loadedAssemblies = new()
        {
            Assembly.GetAssembly(typeof(int)) // mscorlib
        };

        public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

        internal Binder(BinderFlags binderFlags)
        {
            this.binderFlags = binderFlags;

            var allTypes = loadedAssemblies.SelectMany(a => a.GetTypes());
            this.loadedTypes = allTypes.ToDictionary(t => t.FullName, t => t);

            var n = allTypes
                .Where(t => !string.IsNullOrEmpty(t.Namespace))
                .Select(t => t.Namespace);
            this.loadedNamespaces = NamespaceUtilities.GetFullNamespaces(n);
        }

        private BoundErrorExpression ReportErrorExpression(Diagnostic diagnostic)
        {
            this.diagnostics.Add(diagnostic);
            return new BoundErrorExpression();
        }
    }
}
