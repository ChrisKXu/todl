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

        private SyntaxToken Seek(int offset)
        {
            var index = this.position + offset;
            if (index >= this.SyntaxTokens.Count)
            {
                return this.SyntaxTokens.Last();
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

            while (Current.Kind != SyntaxKind.EndOfFileToken)
            {
                ParseExpression();
            }
        }

        private Expression ParsePrimaryExpression()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.NumberToken:
                case SyntaxKind.StringToken:
                case SyntaxKind.TrueKeywordToken:
                case SyntaxKind.FalseKeywordToken:
                    return new LiteralExpression(this.syntaxTree, this.ExpectToken(Current.Kind));
                case SyntaxKind.LeftParenthesisToken:
                    return this.ParseTrailingUnaryExpression(this.ParseParethesizedExpression());
                case SyntaxKind.IdentifierToken:
                    if (AssignmentExpression.AssignmentOperators.Contains(Peak.Kind))
                    {
                        return this.ParseAssignmentExpression();
                    }

                    return this.ParseTrailingUnaryExpression(new NameExpression(this.syntaxTree, this.ExpectToken(SyntaxKind.IdentifierToken)));
                default:
                    return this.ParseTrailingUnaryExpression(new NameExpression(this.syntaxTree, this.ExpectToken(SyntaxKind.IdentifierToken)));
            }
        }

        private void ReportUnexpectedToken(SyntaxKind expectedSyntaxKind)
        {
            this.diagnostics.Add(new Diagnostic($"Unexpected token found: {Current.Text}. Expecting {expectedSyntaxKind}", DiagnosticLevel.Error, Current.GetTextLocation()));
        }
    }
}
