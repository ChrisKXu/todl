using System;
using System.Collections.Generic;
using System.Linq;
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
        public IReadOnlyList<Member> Members => parser.Members;
        public ClrTypeCacheView ClrTypeCacheView { get; private set; }

        internal SyntaxTree(SourceText sourceText, ClrTypeCache clrTypeCache)
        {
            lexer = new Lexer() { SourceText = sourceText };
            parser = new Parser(this);

            SourceText = sourceText;
            ClrTypeCache = clrTypeCache;
        }

        private Expression ParseExpression()
        {
            lexer.Lex();
            ClrTypeCacheView = ClrTypeCache.CreateView(Array.Empty<ImportDirective>());
            return parser.ParseExpression();
        }

        private Statement ParseStatement()
        {
            lexer.Lex();
            ClrTypeCacheView = ClrTypeCache.CreateView(Array.Empty<ImportDirective>());
            return parser.ParseStatement();
        }

        private void Parse()
        {
            lexer.Lex();
            parser.Parse();

            ClrTypeCacheView = ClrTypeCache.CreateView(Directives.OfType<ImportDirective>());
        }

        public static SyntaxTree Parse(SourceText sourceText, ClrTypeCache clrTypeCache)
        {
            var syntaxTree = new SyntaxTree(sourceText, clrTypeCache);
            syntaxTree.Parse();
            return syntaxTree;
        }

        // temporarily make available for tests and evaluator
        internal static Expression ParseExpression(SourceText sourceText, ClrTypeCache clrTypeCache)
        {
            var syntaxTree = new SyntaxTree(sourceText, clrTypeCache);
            return syntaxTree.ParseExpression();
        }

        internal static Statement ParseStatement(SourceText sourceText, ClrTypeCache clrTypeCache)
        {
            var syntaxTree = new SyntaxTree(sourceText, clrTypeCache);
            return syntaxTree.ParseStatement();
        }
    }
}
