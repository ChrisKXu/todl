using System;
using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax
{
    /// <summary>
    /// This lexer converts source texts into syntax tokens, which allows parser to parse into syntax trees later
    /// </summary>
    public sealed class Lexer
    {
        private readonly SyntaxTree syntaxTree;
        private readonly List<SyntaxToken> syntaxTokens = new List<SyntaxToken>();
        private readonly List<Diagnostic> diagnostics = new List<Diagnostic>();
        private int position = 0;

        private SourceText SourceText => this.syntaxTree.SourceText;
        private char Current => this.Seek(0);
        private char Peak => this.Seek(1);

        public IReadOnlyList<SyntaxToken> SyntaxTokens => syntaxTokens;
        public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

        public Lexer(SyntaxTree syntaxTree)
        {
            this.syntaxTree = syntaxTree;
        }

        private char Seek(int offset)
        {
            var index = this.position + offset;

            if (index >= this.SourceText.Text.Length)
            {
                return '\0';
            }

            return this.SourceText.Text[index];
        }

        private IReadOnlyCollection<SyntaxTrivia> ReadSyntaxTrivia(bool leading)
        {
            var triviaList = new List<SyntaxTrivia>();

            var done = false;
            var start = this.position;
            var kind = SyntaxKind.BadToken;

            while(!done)
            {
                switch(Current)
                {
                    case '\0':
                        done = true;
                        break;
                    case '\n': case '\r':
                        done = !leading;
                        kind = SyntaxKind.LineBreakTrivia;
                        ReadLineBreak();
                        break;
                    case ' ': case '\t':
                        kind = SyntaxKind.WhitespaceTrivia;
                        ReadWhitespace();
                        break;
                    default:
                        done = true;
                        break;
                }

                var length = this.position - start;

                if (length > 0)
                {
                    triviaList.Add(new SyntaxTrivia(
                        kind: kind,
                        text: this.SourceText.GetTextSpan(start, length)
                    ));
                }
            }

            return triviaList;
        }

        private void ReadLineBreak()
        {
            if (Current == '\r' && Peak == '\n')
            {
                this.position += 2;
            }
            else
            {
                ++this.position;
            }
        }

        private void ReadWhitespace()
        {
            while (true)
            {
                if (char.IsWhiteSpace(Current))
                {
                    ++this.position;
                }

                break;
            }
        }

        private void ReadNumber()
        {
            // currently we only support integers (123) or floating points in 123.45 format
            // will revisit this part and support other formats as well
            while(true)
            {
                if (char.IsDigit(Current) || Current == '.')
                {
                    ++this.position;
                }
                else
                {
                    break;
                }
            }
        }

        private IReadOnlyCollection<SyntaxTrivia> ReadLeadingSyntaxTrivia() => this.ReadSyntaxTrivia(true);
        private IReadOnlyCollection<SyntaxTrivia> ReadTrailingSyntaxTrivia() => this.ReadSyntaxTrivia(false);

        private SyntaxToken GetNextToken()
        {
            var leadingTrivia = ReadLeadingSyntaxTrivia();

            // read token
            var start = this.position;
            var kind = SyntaxKind.BadToken;

            switch (Current)
            {
                case '\0':
                    kind = SyntaxKind.EndOfFileToken;
                    break;
                case '0': case '1': case '2': case '3': case '4':
                case '5': case '6': case '7': case '8': case '9':
                    kind = SyntaxKind.NumberToken;
                    ReadNumber();
                    break;
                case '+':
                    if (Peak == '+')
                    {
                        kind = SyntaxKind.PlusPlusToken;
                        this.position += 2;
                    }
                    else if (Peak == '=')
                    {
                        kind = SyntaxKind.PlusEqualsToken;
                        this.position += 2;
                    }
                    else
                    {
                        kind = SyntaxKind.PlusToken;
                        ++this.position;
                    }
                    break;
                case '-':
                    if (Peak == '-')
                    {
                        kind = SyntaxKind.MinusMinusToken;
                        this.position += 2;
                    }
                    else if (Peak == '=')
                    {
                        kind = SyntaxKind.MinusEqualsToken;
                        this.position += 2;
                    }
                    else
                    {
                        kind = SyntaxKind.MinusToken;
                        ++this.position;
                    }
                    break;
                case '*':
                    if (Peak == '=')
                    {
                        kind = SyntaxKind.StarEqualsToken;
                        this.position += 2;
                    }
                    else
                    {
                        kind = SyntaxKind.StarToken;
                        ++this.position;
                    }
                    break;
                case '/':
                    if (Peak == '=')
                    {
                        kind = SyntaxKind.SlashEqualsToken;
                        this.position += 2;
                    }
                    else
                    {
                        kind = SyntaxKind.SlashToken;
                        ++this.position;
                    }
                    break;
                case '(':
                    kind = SyntaxKind.LeftParenthesisToken;
                    ++this.position;
                    break;
                case ')':
                    kind = SyntaxKind.RightParenthesisToken;
                    ++this.position;
                    break;
                case '=':
                    if (Peak == '=')
                    {
                        kind = SyntaxKind.EqualsEqualsToken;
                        this.position += 2;
                    }
                    else
                    {
                        kind = SyntaxKind.EqualsToken;
                        ++this.position;
                    }
                    break;
                case '!':
                    if (Peak == '=')
                    {
                        kind = SyntaxKind.BangEqualsToken;
                        this.position += 2;
                    }
                    else
                    {
                        kind = SyntaxKind.BangToken;
                        ++this.position;
                    }
                    break;
                default:
                    break;
            }

            var length = this.position - start;

            var trailingTrivia = ReadTrailingSyntaxTrivia();

            return new SyntaxToken(
                syntaxTree: this.syntaxTree,
                kind: kind,
                text: this.SourceText.GetTextSpan(start, length),
                leadingTrivia: leadingTrivia,
                trailingTrivia: trailingTrivia
            );
        }

        public void Lex()
        {
            // if syntaxTokens is not empty, it means lexer has already been executed.
            if (syntaxTokens.Any())
            {
                return;
            }

            while (true)
            {
                var token = GetNextToken();
                syntaxTokens.Add(token);

                if (token.Kind == SyntaxKind.BadToken)
                {
                    diagnostics.Add(new Diagnostic("Bad token", DiagnosticLevel.Error, token.GetTextLocation()));
                    break;
                }

                if (token.Kind == SyntaxKind.EndOfFileToken)
                {
                    break;
                }
            }
        }
    }
}