using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
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
        private readonly BoundScope boundScope;
        private readonly BinderFlags binderFlags;

        public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

        internal Binder(BoundScope boundScope, BinderFlags binderFlags)
        {
            this.boundScope = boundScope;
            this.binderFlags = binderFlags;
        }

        private BoundErrorExpression ReportErrorExpression(Diagnostic diagnostic)
        {
            this.diagnostics.Add(diagnostic);
            return new BoundErrorExpression();
        }
    }
}
