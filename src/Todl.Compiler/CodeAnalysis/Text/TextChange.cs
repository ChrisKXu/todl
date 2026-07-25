namespace Todl.Compiler.CodeAnalysis.Text;

/// <summary>
/// A single replacement of <see cref="Span"/> (against the old text) with <see cref="NewText"/>.
/// Insertion: Span.Length == 0. Deletion: NewText == "".
/// </summary>
public readonly record struct TextChange(TextSpan Span, string NewText)
{
    public int Delta => NewText.Length - Span.Length;
}
