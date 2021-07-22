﻿using System.Collections.Generic;
using System.Reflection;
using Todl.Compiler.Diagnostics;

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
        private readonly List<Assembly> loadedAssemblies = new()
        {
            Assembly.GetAssembly(typeof(int)) // mscorlib
        };

        public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

        internal Binder(BinderFlags binderFlags)
        {
            this.binderFlags = binderFlags;
        }

        private BoundErrorExpression ReportErrorExpression(Diagnostic diagnostic)
        {
            this.diagnostics.Add(diagnostic);
            return new BoundErrorExpression();
        }
    }
}
