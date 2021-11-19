namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public abstract class Directive : SyntaxNode { }

    public sealed partial class Parser
    {
        private Directive ParseDirective()
            => ParseImportDirective();
    }
}
