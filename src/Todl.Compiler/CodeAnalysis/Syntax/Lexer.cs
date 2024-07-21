using System.Collections.Generic;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeAnalysis.Syntax;

/// <summary>
/// This lexer converts source texts into syntax tokens, which allows parser to parse into syntax trees later
/// </summary>
internal sealed class Lexer
{
    private readonly List<SyntaxToken> syntaxTokens = new();
    private int position = 0;

    public SourceText SourceText { get; internal set; }

    private char Current => Seek(0);
    private char Peak => Seek(1);

    public IReadOnlyList<SyntaxToken> SyntaxTokens => syntaxTokens;

    private char Seek(int offset)
    {
        var index = this.position + offset;

        if (index >= this.SourceText.Text.Length)
        {
            return '\0';
        }

        return this.SourceText.Text[index];
    }

    private IReadOnlyList<SyntaxTrivia> ReadSyntaxTrivia(bool leading)
    {
        var triviaList = new List<SyntaxTrivia>();

        var done = false;
        var start = this.position;
        var kind = SyntaxKind.BadToken;

        while (!done)
        {
            switch (Current)
            {
                case '\0':
                    done = true;
                    break;
                case '\n':
                case '\r':
                    done = !leading;
                    kind = SyntaxKind.LineBreakTrivia;
                    ReadLineBreak();
                    break;
                case ' ':
                case '\t':
                    kind = SyntaxKind.WhitespaceTrivia;
                    ReadWhitespace();
                    break;
                case '/':
                    if (Peak == '/')
                    {
                        kind = SyntaxKind.SingleLineCommentTrivia;
                        ReadSingleLineComment();
                    }
                    else
                    {
                        done = true;
                    }
                    break;
                default:
                    done = true;
                    break;
            }

            var length = position - start;

            if (length > 0)
            {
                triviaList.Add(
                    new SyntaxTrivia(kind, SourceText.GetTextSpan(start, length)));
            }

            start = position;
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

    private void ReadSingleLineComment()
    {
        position += 2; // '//'
        while (Current != '\r' && Current != '\n' && Current != '\0')
        {
            ++position;
        }
    }

    private SyntaxKind ReadNumericLiteral()
    {
        while (char.IsDigit(Current)
                || (Current == '.' && char.IsDigit(Peak)))
        {
            ++position;
        }

        if (Current == 'f' || Current == 'F' || Current == 'd' || Current == 'D')
        {
            ++position;
        }
        else
        {
            if (Current == 'u' || Current == 'U')
            {
                ++position;
            }

            if (Current == 'l' || Current == 'L')
            {
                ++position;
            }
        }

        return SyntaxKind.NumberToken;
    }

    private SyntaxKind ReadBinaryNumericLiteral()
    {
        position += 2;
        while (Current == '0' || Current == '1')
        {
            ++position;
        }

        if (Current == 'u' || Current == 'U')
        {
            ++position;
        }

        if (Current == 'l' || Current == 'L')
        {
            ++position;
        }

        return SyntaxKind.NumberToken;
    }

    private SyntaxKind ReadHexadecimalNumericLiteral()
    {
        position += 2;

        while (char.IsDigit(Current)
            || (Current >= 'a' && Current <= 'f')
            || (Current >= 'A' && Current <= 'F'))
        {
            ++position;
        }

        if (Current == 'u' || Current == 'U')
        {
            ++position;
        }

        if (Current == 'l' || Current == 'L')
        {
            ++position;
        }

        return SyntaxKind.NumberToken;
    }

    private (SyntaxKind, ErrorCode) ReadString()
    {
        switch (Current)
        {
            case '"':
                ++this.position;
                break;
            case '@':
                this.position += 2;
                break;
        }

        var done = false;

        while (!done)
        {
            switch (Current)
            {
                case '\0':
                case '\r':
                case '\n':
                    return (SyntaxKind.BadToken, ErrorCode.UnexpectedEndOfFile);
                case '"':
                    ++this.position;
                    done = true;
                    break;
                case '\\':
                    if (Peak == '\0')
                    {
                        return (SyntaxKind.BadToken, ErrorCode.UnexpectedEndOfFile);
                    }

                    this.position += 2;
                    break;
                default:
                    ++this.position;
                    break;
            }
        }

        return (SyntaxKind.StringToken, ErrorCode.Invalid);
    }

    private SyntaxKind ReadKeywordOrIdentifier()
    {
        var start = this.position;

        while (char.IsLetterOrDigit(Current))
        {
            ++this.position;
        }

        var token = this.SourceText.Text[start..this.position];
        return SyntaxFacts.KeywordMap.GetValueOrDefault(token, SyntaxKind.IdentifierToken);
    }

    private IReadOnlyList<SyntaxTrivia> ReadLeadingSyntaxTrivia() => ReadSyntaxTrivia(true);
    private IReadOnlyList<SyntaxTrivia> ReadTrailingSyntaxTrivia() => ReadSyntaxTrivia(false);

    private SyntaxToken GetNextToken()
    {
        var leadingTrivia = ReadLeadingSyntaxTrivia();

        // read token
        var start = this.position;
        var kind = SyntaxKind.BadToken;
        var errorCode = ErrorCode.UnrecognizedToken;

        switch (Current)
        {
            case '\0':
                kind = SyntaxKind.EndOfFileToken;
                break;
            case '0':
                if (Peak == 'b' || Peak == 'B')
                {
                    kind = ReadBinaryNumericLiteral();
                }
                else if (Peak == 'x' || Peak == 'X')
                {
                    kind = ReadHexadecimalNumericLiteral();
                }
                else
                {
                    kind = ReadNumericLiteral();
                }
                break;
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                kind = ReadNumericLiteral();
                break;
            case '"':
                (kind, errorCode) = ReadString();
                break;
            case '@':
                if (Peak == '"')
                {
                    (kind, errorCode) = ReadString();
                }
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
                else if (Peak == '/')
                {
                    // We always process SingleLineCommentTrivia
                    // at the end of a token
                    break;
                }
                else
                {
                    kind = SyntaxKind.SlashToken;
                    ++this.position;
                }
                break;
            case '(':
                kind = SyntaxKind.OpenParenthesisToken;
                ++this.position;
                break;
            case ')':
                kind = SyntaxKind.CloseParenthesisToken;
                ++this.position;
                break;
            case '{':
                kind = SyntaxKind.OpenBraceToken;
                ++this.position;
                break;
            case '}':
                kind = SyntaxKind.CloseBraceToken;
                ++this.position;
                break;
            case '[':
                kind = SyntaxKind.OpenBracketToken;
                ++position;
                break;
            case ']':
                kind = SyntaxKind.CloseBracketToken;
                ++position;
                break;
            case ';':
                kind = SyntaxKind.SemicolonToken;
                ++this.position;
                break;
            case ':':
                kind = SyntaxKind.ColonToken;
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
            case '<':
                if (Peak == '=')
                {
                    kind = SyntaxKind.LessThanOrEqualsToken;
                    position += 2;
                }
                else
                {
                    kind = SyntaxKind.LessThanToken;
                    ++position;
                }
                break;
            case '>':
                if (Peak == '=')
                {
                    kind = SyntaxKind.GreaterThanOrEqualsToken;
                    position += 2;
                }
                else
                {
                    kind = SyntaxKind.GreaterThanToken;
                    ++position;
                }
                break;
            case '&':
                if (Peak == '&')
                {
                    kind = SyntaxKind.AmpersandAmpersandToken;
                    this.position += 2;
                }
                else
                {
                    kind = SyntaxKind.AmpersandToken;
                    ++this.position;
                }
                break;
            case '|':
                if (Peak == '|')
                {
                    kind = SyntaxKind.PipePipeToken;
                    this.position += 2;
                }
                else
                {
                    kind = SyntaxKind.PipeToken;
                    ++this.position;
                }
                break;
            case '~':
                kind = SyntaxKind.TildeToken;
                ++this.position;
                break;
            case '.':
                if (char.IsDigit(Peak))
                {
                    kind = ReadNumericLiteral();
                    break;
                }
                kind = SyntaxKind.DotToken;
                ++this.position;
                break;
            case ',':
                kind = SyntaxKind.CommaToken;
                ++this.position;
                break;
            default:
                // identifiers in todl can only start with a letter and not a digit or underscore
                if (char.IsLetter(Current))
                {
                    kind = ReadKeywordOrIdentifier();
                }

                break;
        }

        var length = this.position - start;

        var trailingTrivia = ReadTrailingSyntaxTrivia();
        var text = SourceText.GetTextSpan(start, length);

        if (kind == SyntaxKind.BadToken)
        {
            return new()
            {
                Kind = kind,
                Text = text,
                LeadingTrivia = leadingTrivia,
                TrailingTrivia = trailingTrivia,
                ErrorCode = errorCode
            };
        }

        return new()
        {
            Kind = kind,
            Text = text,
            LeadingTrivia = leadingTrivia,
            TrailingTrivia = trailingTrivia
        };
    }

    public void Lex()
    {
        // if syntaxTokens is not empty, it means lexer has already been executed.
        if (syntaxTokens.Any())
        {
            return;
        }

        SyntaxToken token;

        do
        {
            token = GetNextToken();
            syntaxTokens.Add(token);
        }
        while (token.Kind != SyntaxKind.BadToken
            && token.Kind != SyntaxKind.EndOfFileToken);
    }
}
