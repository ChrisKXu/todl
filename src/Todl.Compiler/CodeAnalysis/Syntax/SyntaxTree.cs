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
        public IReadOnlyList<Member> Statements => parser.Members;
        public ClrTypeCache ClrTypeCache { get; }

        internal SyntaxTree(SourceText sourceText)
        {
            parser = new Parser(this);

            SourceText = sourceText;
            ClrTypeCache = ClrTypeCache.Default;
        }

        private void Parse() => parser.Parse();

        public static SyntaxTree Parse(SourceText sourceText)
        {
            var syntaxTree = new SyntaxTree(sourceText);
            syntaxTree.Parse();
            return syntaxTree;
        }
    }
}
