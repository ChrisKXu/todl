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
        private readonly ClrTypeCache clrTypeCache = ClrTypeCache.Default;

        public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

        internal Binder(BinderFlags binderFlags)
        {
            this.binderFlags = binderFlags;
        }

        private BoundErrorExpression ReportErrorExpression(Diagnostic diagnostic)
        {
            diagnostics.Add(diagnostic);
            return new BoundErrorExpression();
        }
    }
}
