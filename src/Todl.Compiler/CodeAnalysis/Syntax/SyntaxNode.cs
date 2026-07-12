using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public abstract class SyntaxNode
{
    public SyntaxTree SyntaxTree { get; internal init; }
    public abstract TextSpan Text { get; }

    public string GetText() => SyntaxTree.SourceText.ToString(Text);
    public TextLocation GetTextLocation() => new(SyntaxTree.SourceText, Text);
    public TextLocation GetTextLocation(TextSpan span) => new(SyntaxTree.SourceText, span);
}
