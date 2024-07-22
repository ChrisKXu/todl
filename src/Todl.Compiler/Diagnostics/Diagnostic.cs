using System.Diagnostics;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.Diagnostics
{
    [DebuggerDisplay("{GetDebuggerDisplay()}")]
    public readonly struct Diagnostic
    {
        public string Message { get; init; }
        public DiagnosticLevel Level { get; init; }
        public TextLocation TextLocation { get; init; }
        public ErrorCode ErrorCode { get; init; }

        public string GetDebuggerDisplay()
            => $"ErrorCode = {ErrorCode}, Text = \"{TextLocation.TextSpan}\"";
    }
}
