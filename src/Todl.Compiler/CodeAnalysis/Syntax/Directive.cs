namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public abstract class Directive : SyntaxNode { }

    public sealed partial class Parser
    {
        public Directive ParseDirective()
            => ParseImportDirective();
    }
}
