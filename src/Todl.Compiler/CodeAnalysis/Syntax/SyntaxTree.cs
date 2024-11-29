using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class SyntaxTree
{
    private readonly Lexer lexer;
    private readonly Parser parser;

    public SourceText SourceText { get; }
    public ClrTypeCache ClrTypeCache { get; }

    public ImmutableArray<SyntaxToken> SyntaxTokens => lexer.SyntaxTokens;
    public ImmutableArray<Directive> Directives => parser.Directives;
    public ImmutableArray<Member> Members => parser.Members;
    public ClrTypeCacheView ClrTypeCacheView { get; private set; }

    internal SyntaxTree(
        SourceText sourceText,
        ClrTypeCache clrTypeCache,
        DiagnosticBag.Builder diagnosticBuilder)
    {
        lexer = new Lexer() { SourceText = sourceText };
        parser = new Parser(this, diagnosticBuilder);

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

    public static SyntaxTree Parse(
        SourceText sourceText,
        ClrTypeCache clrTypeCache,
        DiagnosticBag.Builder diagnosticBuilder)
    {
        var syntaxTree = new SyntaxTree(sourceText, clrTypeCache, diagnosticBuilder);
        syntaxTree.Parse();
        return syntaxTree;
    }

    // temporarily make available for tests and evaluator
    internal static Expression ParseExpression(
        SourceText sourceText,
        ClrTypeCache clrTypeCache,
        DiagnosticBag.Builder diagnosticBuilder)
    {
        var syntaxTree = new SyntaxTree(sourceText, clrTypeCache, diagnosticBuilder);
        return syntaxTree.ParseExpression();
    }

    internal static Statement ParseStatement(
        SourceText sourceText,
        ClrTypeCache clrTypeCache,
        DiagnosticBag.Builder diagnosticBuilder)
    {
        var syntaxTree = new SyntaxTree(sourceText, clrTypeCache, diagnosticBuilder);
        return syntaxTree.ParseStatement();
    }
}
