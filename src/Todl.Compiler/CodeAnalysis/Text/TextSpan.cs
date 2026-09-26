namespace Todl.Compiler.CodeAnalysis.Text
{
    /// <summary>
    /// TextSpan represents a portion of the source text as a pure (Start, Length) coordinate.
    /// It carries no reference to any particular SourceText instance, so spans with equal
    /// (Start, Length) compare equal regardless of which source they were produced from.
    /// Resolving the underlying characters is the responsibility of SourceText/TextLocation.
    /// </summary>
    public readonly record struct TextSpan(int Start, int Length)
    {
        public int End => Start + Length;

        public static TextSpan FromBounds(int start, int end) => new(start, end - start);
    }
}
