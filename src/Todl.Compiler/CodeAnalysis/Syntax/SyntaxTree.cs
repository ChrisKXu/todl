﻿using System.Collections.Immutable;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax;

public sealed class SyntaxTree
{
    private readonly Lexer lexer;
    private readonly Parser parser;

    public SourceText SourceText { get; }

    public ImmutableArray<SyntaxToken> SyntaxTokens => lexer.SyntaxTokens;
    public ImmutableArray<Directive> Directives => parser.Directives;
    public ImmutableArray<Member> Members => parser.Members;

    internal SyntaxTree(
        SourceText sourceText,
        DiagnosticBag.Builder diagnosticBuilder)
    {
        lexer = new Lexer() { SourceText = sourceText };
        parser = new Parser(this, diagnosticBuilder);

        SourceText = sourceText;
    }

    private Expression ParseExpression()
    {
        lexer.Lex();
        return parser.ParseExpression();
    }

    private Statement ParseStatement()
    {
        lexer.Lex();
        return parser.ParseStatement();
    }

    private void Parse()
    {
        lexer.Lex();
        parser.Parse();
    }

    public static SyntaxTree Parse(
        SourceText sourceText,
        DiagnosticBag.Builder diagnosticBuilder)
    {
        var syntaxTree = new SyntaxTree(sourceText, diagnosticBuilder);
        syntaxTree.Parse();
        return syntaxTree;
    }

    // temporarily make available for tests and evaluator
    internal static Expression ParseExpression(
        SourceText sourceText,
        DiagnosticBag.Builder diagnosticBuilder)
    {
        var syntaxTree = new SyntaxTree(sourceText, diagnosticBuilder);
        return syntaxTree.ParseExpression();
    }

    internal static Statement ParseStatement(
        SourceText sourceText,
        DiagnosticBag.Builder diagnosticBuilder)
    {
        var syntaxTree = new SyntaxTree(sourceText, diagnosticBuilder);
        return syntaxTree.ParseStatement();
    }
}
