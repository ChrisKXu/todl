namespace Todl.Compiler.CodeAnalysis.Text;

public record struct TextLocation(SourceText SourceText, TextSpan TextSpan)
{
    public readonly string GetText() => SourceText?.ToString(TextSpan);
}
