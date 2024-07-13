using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

internal abstract class BoundNode : IDiagnosable
{
    public SyntaxNode SyntaxNode { get; internal init; }
    public DiagnosticBag.Builder DiagnosticBuilder { get; internal init; }

    public abstract BoundNode Accept(BoundTreeVisitor visitor);

    public virtual IEnumerable<Diagnostic> GetDiagnostics()
    {
        if (DiagnosticBuilder == null)
        {
            return DiagnosticBag.Empty;

        }

        return DiagnosticBuilder.Build();
    }
}
