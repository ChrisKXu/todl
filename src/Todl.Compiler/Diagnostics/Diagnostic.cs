using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.Diagnostics
{
    public sealed class Diagnostic
    {
        public string Message { get; }
        public DiagnosticLevel Level { get; }
        public TextLocation TextLocation { get; }

        public Diagnostic(string message, DiagnosticLevel level, TextLocation textLocation)
        {
            this.Message = message;
            this.Level = level;
            this.TextLocation = textLocation;
        }
    }
}