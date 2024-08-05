using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public record struct SyntaxToken
(
    SyntaxKind Kind,
    TextSpan Text,
    ImmutableArray<SyntaxTrivia> LeadingTrivia,
    ImmutableArray<SyntaxTrivia> TrailingTrivia,
    bool Missing,
    ErrorCode ErrorCode
) : IDiagnosable
{
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

        return new()
        {
            Message = message,
            ErrorCode = ErrorCode,
            TextLocation = GetTextLocation(),
            Level = DiagnosticLevel.Error
        };
    }

    public TextLocation GetTextLocation() => new() { TextSpan = Text };
}
