using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.Diagnostics
{
    public sealed class Diagnostic
    {
        public string Message { get; init; }
        public DiagnosticLevel Level { get; init; }
        public TextLocation TextLocation { get; init; }
        public ErrorCode ErrorCode { get; init; }
    }
}
