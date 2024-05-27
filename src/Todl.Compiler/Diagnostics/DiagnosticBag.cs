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

        public void Add(Diagnostic diagnostic)
            => Diagnostics.Add(diagnostic);

        public void Add(IDiagnosable diagnosable)
        {
            if (diagnosable is not null)
            {
                Diagnostics.AddRange(diagnosable.GetDiagnostics());
            }
        }

        public void AddRange(IEnumerable<Diagnostic> diagnostics)
            => Diagnostics.AddRange(diagnostics);

        public void AddRange(params Diagnostic[] diagnostics)
            => AddRange(diagnostics.AsEnumerable());

        public void AddRange(IEnumerable<IDiagnosable> diagnosables)
        {
            if (diagnosables is not null)
            {
                AddRange(diagnosables.Where(d => d != null).SelectMany(d => d.GetDiagnostics()));
            }
        }

        public void AddRange(params IDiagnosable[] diagnosables)
            => AddRange(diagnosables.AsEnumerable());

        public void AddRange<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> diagnosables)
            where TValue : IDiagnosable
            => AddRange(diagnosables?.Values.OfType<IDiagnosable>());
    }

    public static readonly DiagnosticBag Empty = new(Enumerable.Empty<Diagnostic>());

    private readonly ImmutableList<Diagnostic> diagnostics;

    private DiagnosticBag(IEnumerable<Diagnostic> unsortedDiagnostics)
    {
        diagnostics = unsortedDiagnostics
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
