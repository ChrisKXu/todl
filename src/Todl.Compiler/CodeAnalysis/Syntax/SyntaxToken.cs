using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public readonly struct SyntaxToken : IDiagnosable
    {
        public SyntaxKind Kind { get; internal init; }
        public TextSpan Text { get; internal init; }
        public IReadOnlyCollection<SyntaxTrivia> LeadingTrivia { get; internal init; }
        public IReadOnlyCollection<SyntaxTrivia> TrailingTrivia { get; internal init; }
        public bool Missing { get; internal init; }
        public ErrorCode ErrorCode { get; internal init; }

        public IEnumerable<Diagnostic> GetDiagnostics()
        {
            if (Kind != SyntaxKind.BadToken)
            {
                return Enumerable.Empty<Diagnostic>();
            }

            return DiagnosticBag.FromSingle(GetDiagnosticFromErrorCode());
        }

        private Diagnostic GetDiagnosticFromErrorCode()
        {
            var message = ErrorCode switch
            {
                ErrorCode.UnrecognizedToken => $"Token '{Text}' is not recognized",
                ErrorCode.UnexpectedEndOfFile => "Unexpected EndOfFileToken",
                ErrorCode.UnexpectedToken => $"Unexpected token found: {Text}. Expecting {Kind}",
                _ => string.Empty
            };

            return new Diagnostic()
            {
                Message = message,
                ErrorCode = ErrorCode,
                TextLocation = GetTextLocation(),
                Level = DiagnosticLevel.Error
            };
        }

        public TextLocation GetTextLocation() => new() { TextSpan = Text };
    }
}
