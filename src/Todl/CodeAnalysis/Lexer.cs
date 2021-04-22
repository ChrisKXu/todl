using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Todl.Diagnostics;

namespace Todl.CodeAnalysis
{
    /// <summary>
    /// This lexer converts source texts into syntax tokens, which allows parser to parse into syntax trees later
    /// </summary>
    public sealed class Lexer
    {
        private readonly SourceText sourceText;
        private readonly ImmutableArray<SyntaxToken>.Builder syntaxTokenBuilder = ImmutableArray.CreateBuilder<SyntaxToken>();
        private readonly ImmutableArray<Diagnostic>.Builder diagnosticsBuilder = ImmutableArray.CreateBuilder<Diagnostic>();
        private int position = 0;

        private char Current => this.Seek(0);

        private char Peak => this.Seek(1);

        public IReadOnlyCollection<SyntaxToken> SyntaxTokens => syntaxTokenBuilder.ToImmutable();

        public IReadOnlyCollection<Diagnostic> Diagnostics => diagnosticsBuilder.ToImmutable();

        public Lexer(SourceText sourceText)
        {
            this.sourceText = sourceText;
        }

        private char Seek(int offset)
        {
            var index = this.position + offset;

            if (index >= this.sourceText.Text.Length)
            {
                return '\0';
            }

            return this.sourceText.Text[index];
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
                        text: this.sourceText.Text.Substring(start, length),
                        position: start
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
                default:
                    break;
            }

            var length = this.position - start;

            var trailingTrivia = ReadTrailingSyntaxTrivia();

            return new SyntaxToken(
                kind: kind,
                text: this.sourceText.Text.Substring(start, length),
                position: start,
                leadingTrivia: leadingTrivia,
                trailingTrivia: trailingTrivia
            );
        }

        public void Lex()
        {
            while (true)
            {
                var token = GetNextToken();
                syntaxTokenBuilder.Add(token);

                if (token.Kind == SyntaxKind.BadToken)
                {
                    diagnosticsBuilder.Add(new Diagnostic("Bad token", DiagnosticLevel.Error));
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