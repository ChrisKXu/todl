namespace Todl.Compiler.CodeAnalysis.Syntax;

public abstract class Expression : SyntaxNode { }

public sealed partial class Parser
{
    internal Expression ParseExpression()
        => ParseBinaryExpression();
}
