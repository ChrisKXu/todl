namespace Todl.Compiler.CodeAnalysis.Text;

/// <summary>
/// The span of the old text that <see cref="TextChange"/>s replaced, and the length of its
/// replacement in the new text. Unlike <see cref="TextChange"/>, this carries no content -
/// only enough to describe what region became invalid.
/// </summary>
public readonly record struct TextChangeRange(TextSpan Span, int NewLength);
