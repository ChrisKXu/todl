using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Todl.Compiler.Diagnostics;

public sealed class DiagnosticBag : IEnumerable<Diagnostic>
{
    public sealed class Builder
    {
        public readonly List<Diagnostic> Diagnostics = new();

        public DiagnosticBag Build() => new(Diagnostics);
    }

    public static readonly DiagnosticBag Empty = new(Enumerable.Empty<Diagnostic>());

    private readonly ImmutableList<Diagnostic> diagnostics;

    private DiagnosticBag(IEnumerable<Diagnostic> unsortedDiagnostics)
    {
        diagnostics = unsortedDiagnostics
            .Where(d => d != null)
            .OrderBy(d => d.Level)
            .ThenBy(d => d.TextLocation.TextSpan.Start)
            .ToImmutableList();
    }

    public static DiagnosticBag FromSingle(Diagnostic diagnostic)
        => new(new[] { diagnostic });

    public IEnumerator<Diagnostic> GetEnumerator()
        => diagnostics.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => diagnostics.GetEnumerator();
}
