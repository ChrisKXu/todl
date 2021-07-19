namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public abstract class Directive : SyntaxNode
    {
        protected Directive(SyntaxTree syntaxTree) : base(syntaxTree) { }
    }

    public sealed partial class Parser
    {
        public Directive ParseDirective()
        {
            return ParseImportDirective();
        }
    }
}
