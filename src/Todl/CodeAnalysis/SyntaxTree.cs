using System;

namespace Todl.CodeAnalysis
{
    public sealed class SyntaxTree
    {
        private readonly Parser parser;

        public SourceText SourceText { get; }

        public SyntaxTree(SourceText sourceText)
        {
            this.SourceText = sourceText;
            this.parser = new Parser(this);
        }

        private void Parse()
        {
            this.parser.Parse();
        }

        public static SyntaxTree Parse(SourceText sourceText)
        {
            var syntaxTree = new SyntaxTree(sourceText);
            syntaxTree.Parse();
            return syntaxTree;
        }
    }
}