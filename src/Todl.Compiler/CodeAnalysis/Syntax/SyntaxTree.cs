using System;
using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    public sealed class SyntaxTree
    {
        private readonly Lexer lexer;
        private readonly Parser parser;

        public SourceText SourceText { get; }
        public ClrTypeCache ClrTypeCache { get; }

        public IReadOnlyList<SyntaxToken> SyntaxTokens => lexer.SyntaxTokens;
        public IReadOnlyList<Directive> Directives => parser.Directives;
        public IReadOnlyList<Member> Statements => parser.Members;

        internal SyntaxTree(SourceText sourceText)
        {
            lexer = new Lexer() { SourceText = sourceText };
            parser = new Parser(this);

            SourceText = sourceText;
            ClrTypeCache = ClrTypeCache.Default;
        }

        // make available to unit test
        internal void Lex() => lexer.Lex();

        private void Parse()
        {
            Lex();
            parser.Parse();
        }

        public static SyntaxTree Parse(SourceText sourceText)
        {
            var syntaxTree = new SyntaxTree(sourceText);
            syntaxTree.Parse();
            return syntaxTree;
        }
    }
}
