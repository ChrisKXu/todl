using System;
using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class SyntaxTree
    {
        private readonly Parser parser;

        public SourceText SourceText { get; }
        public IReadOnlyList<Directive> Directives => parser.Directives;
        public IReadOnlyList<Statement> Statements => parser.Statements;

        internal SyntaxTree(SourceText sourceText)
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
