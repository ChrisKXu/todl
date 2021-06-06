namespace Todl.CodeAnalysis.Text
{
    public struct TextLocation
    {
        public SourceText SourceText { get; }
        public TextSpan TextSpan { get; }

        public TextLocation(SourceText sourceText, TextSpan textSpan)
        {
            this.SourceText = sourceText;
            this.TextSpan = textSpan;
        }
    }
}
