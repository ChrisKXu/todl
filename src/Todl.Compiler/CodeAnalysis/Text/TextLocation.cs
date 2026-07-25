namespace Todl.Compiler.CodeAnalysis.Text;

public record struct TextLocation(SourceText SourceText, TextSpan TextSpan)
{
    public readonly string GetText() => SourceText?.ToString(TextSpan) ?? "";

    public int LineNumber => SourceText is not null ? SourceText.GetLinePosition(TextSpan.Start).Line : 0;

    public int CharacterNumber => SourceText is not null ? SourceText.GetLinePosition(TextSpan.Start).Character : 0;

    public LinePosition GetStartLinePosition()
        => SourceText is not null ? SourceText.GetLinePosition(TextSpan.Start) : default;

    public LinePosition GetEndLinePosition()
        => SourceText is not null ? SourceText.GetLinePosition(TextSpan.End) : default;

    public LinePositionSpan GetLinePositionSpan()
    {
        if (SourceText is null)
            return new LinePositionSpan(default, default);
        return new LinePositionSpan(GetStartLinePosition(), GetEndLinePosition());
    }
}
