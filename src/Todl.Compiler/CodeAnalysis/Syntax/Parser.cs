using System;
using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    /// <summary>
    /// This class implements a recursive descent parser for the todl language
    /// </summary>
    public sealed partial class Parser
    {
        private readonly SyntaxTree syntaxTree;
        private readonly Lexer lexer;
        private readonly List<Diagnostic> diagnostics = new();
        private readonly List<Directive> directives = new();
        private readonly List<Statement> statements = new();
        private int position = 0;

        private IReadOnlyList<SyntaxToken> SyntaxTokens => this.lexer.SyntaxTokens;

        private SyntaxToken Current => this.Seek(0);
        private SyntaxToken Peak => this.Seek(1);

        public IReadOnlyList<Diagnostic> Diagnostics
        {
            get
            {
                if (this.lexer.Diagnostics.Any())
                {
                    return this.lexer.Diagnostics;
                }

                return this.diagnostics;
            }
        }

        public IReadOnlyList<Directive> Directives => directives;
        public IReadOnlyList<Statement> Statements => statements;

        private SyntaxToken Seek(int offset)
        {
            var index = this.position + offset;
            if (index >= this.SyntaxTokens.Count)
            {
                return this.SyntaxTokens[SyntaxTokens.Count - 1];
            }

            return this.SyntaxTokens[index];
        }

        private SyntaxToken NextToken()
        {
            var current = this.Current;
            ++this.position;
            return current;
        }

        private SyntaxToken ExpectToken(SyntaxKind syntaxKind)
        {
            if (this.Current.Kind == syntaxKind)
            {
                return this.NextToken();
            }

            ReportUnexpectedToken(syntaxKind);

            // return a fake syntax token of the expected kind, with a text span at the current location with 0 length 
            return new SyntaxToken(
                syntaxTree: this.syntaxTree,
                kind: syntaxKind,
                text: this.syntaxTree.SourceText.GetTextSpan(this.Current.GetTextLocation().TextSpan.Start, 0),
                leadingTrivia: Array.Empty<SyntaxTrivia>(),
                trailingTrivia: Array.Empty<SyntaxTrivia>());
        }

        internal Parser(SyntaxTree syntaxTree)
        {
            this.syntaxTree = syntaxTree;
            this.lexer = new Lexer(syntaxTree);
        }

        // Giving unit tests access to lexer.Lex()
        internal void Lex() => this.lexer.Lex();

        public void Parse()
        {
            this.Lex();

            if (this.lexer.Diagnostics.Any())
            {
                return;
            }

            while (Current.Kind == SyntaxKind.ImportKeywordToken)
            {
                directives.Add(ParseDirective());
            }

            while (Current.Kind != SyntaxKind.EndOfFileToken)
            {
                statements.Add(ParseStatement());
            }
        }

        private Expression ParsePrimaryExpression()
        {
            Expression baseExpression;

            switch (Current.Kind)
            {
                case SyntaxKind.NumberToken:
                case SyntaxKind.StringToken:
                case SyntaxKind.TrueKeywordToken:
                case SyntaxKind.FalseKeywordToken:
                    baseExpression = new LiteralExpression(this.syntaxTree, this.ExpectToken(Current.Kind));
                    break;
                case SyntaxKind.OpenParenthesisToken:
                    baseExpression = ParseTrailingUnaryExpression(this.ParseParethesizedExpression());
                    break;
                case SyntaxKind.IdentifierToken:
                default:
                    var nameExpression = new NameExpression(this.syntaxTree, this.ExpectToken(SyntaxKind.IdentifierToken));
                    baseExpression = ParseTrailingUnaryExpression(nameExpression);
                    break;
            }

            while (true)
            {
                if (AssignmentExpression.AssignmentOperators.Contains(Current.Kind))
                {
                    baseExpression = ParseAssignmentExpression(baseExpression);
                }
                else if (Current.Kind == SyntaxKind.DotToken && Peak.Kind == SyntaxKind.IdentifierToken)
                {
                    baseExpression = new MemberAccessExpression(
                        syntaxTree: this.syntaxTree,
                        baseExpression: baseExpression,
                        dotToken: ExpectToken(SyntaxKind.DotToken),
                        memberIdentifierToken: ExpectToken(SyntaxKind.IdentifierToken));
                }
                else if (Current.Kind == SyntaxKind.OpenParenthesisToken)
                {
                    baseExpression = ParseFunctionCallExpression(baseExpression);
                }
                else
                {
                    break;
                }
            }

            return baseExpression;
        }

        private void ReportUnexpectedToken(SyntaxKind expectedSyntaxKind)
        {
            this.diagnostics.Add(
                new Diagnostic()
                {
                    Message = $"Unexpected token found: {Current.Text}. Expecting {expectedSyntaxKind}",
                    Level = DiagnosticLevel.Error,
                    TextLocation = Current.GetTextLocation(),
                    ErrorCode = ErrorCode.UnexpectedToken
                });
        }
    }
}
