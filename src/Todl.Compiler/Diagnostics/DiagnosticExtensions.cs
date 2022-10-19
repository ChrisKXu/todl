using System.Collections.Generic;
using System.Linq;

namespace Todl.Compiler.Diagnostics;

public static class DiagnosticExtensions
{
    public static bool HasError(this IEnumerable<Diagnostic> diagnostics)
        => diagnostics.Any(d => d.Level == DiagnosticLevel.Error);
}
