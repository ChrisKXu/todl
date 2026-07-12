using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public record struct SyntaxToken
(
    SyntaxKind Kind,
    TextSpan Span,
    ReadOnlyMemory<char> Text,
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

    /// <summary>
    /// Builds a TextLocation from this token's Span alone. Tokens carry no reference to their
    /// originating SyntaxTree/SourceText, so the resulting TextLocation cannot resolve source
    /// text on its own. Prefer the enclosing SyntaxNode's GetTextLocation(TextSpan) when a
    /// SourceText-backed TextLocation is required (e.g. for diagnostics).
    /// </summary>
    public TextLocation GetTextLocation() => new(default, Span);
}
