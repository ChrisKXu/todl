using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public abstract class SyntaxNode : IDiagnosable
    {
        public SyntaxTree SyntaxTree { get; internal init; }
        public DiagnosticBag.Builder DiagnosticBuilder { get; internal init; }
        public abstract TextSpan Text { get; }

        public virtual IEnumerable<Diagnostic> GetDiagnostics()
        {
            if (DiagnosticBuilder == null)
            {
                return DiagnosticBag.Empty;
            }

            return DiagnosticBuilder.Build();
        }
    }
}
