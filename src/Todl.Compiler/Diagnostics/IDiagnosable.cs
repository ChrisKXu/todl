using System.Collections.Generic;

namespace Todl.Compiler.Diagnostics;

public interface IDiagnosable
{
    IEnumerable<Diagnostic> GetDiagnostics();
}
