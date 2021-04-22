namespace Todl.Diagnostics
{
    public sealed class Diagnostic
    {
        public string Message { get; }
        public DiagnosticLevel Level { get; }
        
        public Diagnostic(string message, DiagnosticLevel level)
        {
            this.Message = message;
            this.Level = level;
        }
    }
}