using System;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class SourceTextLinePositionTests
{
    // ─── LineCount ─────────────────────────────────────────────────────

    [Fact]
    public void LineCount_EmptySource_ReturnsOne()
        => SourceText.FromString("").LineCount.Should().Be(1);

    [Fact]
    public void LineCount_SingleLineNoTrailingNewline_ReturnsOne()
        => SourceText.FromString("hello").LineCount.Should().Be(1);

    [Fact]
    public void LineCount_SingleLineWithTrailingNewline_ReturnsTwo()
        => SourceText.FromString("hello\n").LineCount.Should().Be(2);

    [Fact]
    // "a\nb\nc\n" has 3 newlines → 4 lines (a, b, c, empty)
    public void LineCount_MultipleLinesWithTrailingNewline()
        => SourceText.FromString("a\nb\nc\n").LineCount.Should().Be(4);

    [Fact]
    public void LineCount_NoLineBreaks_ReturnsOne()
        => SourceText.FromString("abc").LineCount.Should().Be(1);

    // ─── \n handling ──────────────────────────────────────────────────

    [Fact]
    public void GetLinePosition_NL_LineBreaks()
    {
        // "func main() {\n print();\n}\n" = 26 chars, newlines at 13, 23, 25
        // line starts: [0, 14, 24, 26], LineCount = 4
        var src = SourceText.FromString("func main() {\n print();\n}\n");
        src.LineCount.Should().Be(4);

        src.GetLinePosition(0).Should().Be(new LinePosition(0, 0));   // 'f' (line 0 char 0)
        src.GetLinePosition(12).Should().Be(new LinePosition(0, 12)); // '{' (line 0 char 12)
        src.GetLinePosition(13).Should().Be(new LinePosition(0, 13)); // '\n' (line 0 char 13)
        src.GetLinePosition(14).Should().Be(new LinePosition(1, 0));  // ' ' space at start of line 1
        src.GetLinePosition(15).Should().Be(new LinePosition(1, 1));  // 'p' (line 1 char 1)
        src.GetLinePosition(24).Should().Be(new LinePosition(2, 0));  // '}' (line 2 char 0)
        src.GetLinePosition(25).Should().Be(new LinePosition(2, 1));  // '\n' (line 2 char 1)
        src.GetOffset(new LinePosition(3, 0)).Should().Be(26);  // trailing empty line offset (beyond text)
    }

    [Fact]
    public void GetLinePosition_NL_InsidePrint()
    {
        var src = SourceText.FromString("func main() {\n print();\n}\n");
        // line starts: [0, 14, 24, 26]; 'p' at offset 15 → line 1 char 1
        src.GetLinePosition(15).Should().Be(new LinePosition(1, 1));  // 'p' (char 1)
        src.GetLinePosition(19).Should().Be(new LinePosition(1, 5));  // 't' in "print" (char 5)
    }

    // ─── \r\n handling ────────────────────────────────────────────────

    [Fact]
    public void GetLinePosition_CRNL_LineBreaks()
    {
        // "a\r\nb\r\nc\r\n" — \r\n at 2,6 → line starts: [0, 3, 6, 9]
        var src = SourceText.FromString("a\r\nb\r\nc\r\n");
        src.LineCount.Should().Be(4);

        src.GetLinePosition(0).Should().Be(new LinePosition(0, 0));   // 'a' (line 0)
        src.GetLinePosition(1).Should().Be(new LinePosition(0, 1));   // '\r' (line 0)
        src.GetLinePosition(2).Should().Be(new LinePosition(0, 2));   // '\n' (line 0)
        src.GetLinePosition(3).Should().Be(new LinePosition(1, 0));   // 'b' (line 1)
        src.GetLinePosition(6).Should().Be(new LinePosition(2, 0));   // 'c' (line 2)

        // GetLinePosition(Length) now correctly returns the trailing empty line
        var eofPos = src.GetLinePosition(src.Length);
        eofPos.Line.Should().Be(3);
        eofPos.Character.Should().Be(0);
    }

    // ─── Lone \r handling ─────────────────────────────────────────────

    [Fact]
    public void GetLinePosition_LoneCR_LineBreaks()
    {
        var src = SourceText.FromString("a\rb\rc");
        src.LineCount.Should().Be(3);

        src.GetLinePosition(0).Should().Be(new LinePosition(0, 0)); // 'a' (line 0)
        src.GetLinePosition(1).Should().Be(new LinePosition(0, 1)); // '\r' (line 0)
        src.GetLinePosition(2).Should().Be(new LinePosition(1, 0)); // 'b' (line 1)
        src.GetLinePosition(4).Should().Be(new LinePosition(2, 0)); // 'c' (line 2)
    }

    // ─── Mixed line breaks ────────────────────────────────────────────

    [Fact]
    public void GetLinePosition_MixedLineBreaks()
    {
        // "a\nb\rc\r\nd" — \n at 1, lone \r at 3, \r\n at 5-6 → line starts: [0, 2, 4, 7]
        var src = SourceText.FromString("a\nb\rc\r\nd");
        src.LineCount.Should().Be(4);

        src.GetLinePosition(0).Should().Be(new LinePosition(0, 0)); // 'a' (line 0)
        src.GetLinePosition(1).Should().Be(new LinePosition(0, 1)); // '\n' (line 0)
        src.GetLinePosition(2).Should().Be(new LinePosition(1, 0)); // 'b' (line 1)
        src.GetLinePosition(3).Should().Be(new LinePosition(1, 1)); // lone \r (line 1)
        src.GetLinePosition(4).Should().Be(new LinePosition(2, 0)); // 'c' (line 2)
        src.GetLinePosition(7).Should().Be(new LinePosition(3, 0)); // 'd' (line 3)
    }

    // ─── Empty source ─────────────────────────────────────────────────

    [Fact]
    public void GetLinePosition_EmptySource()
    {
        SourceText.FromString("").GetLinePosition(0).Should().Be(new LinePosition(0, 0));
    }

    // ─── Clamping at boundaries ───────────────────────────────────────

    [Fact]
    public void GetLinePosition_OffsetBeyondEnd_ClampsToEOF()
        => SourceText.FromString("abc").GetLinePosition(100).Should().Be(new LinePosition(0, 3));

    [Fact]
    public void GetLinePosition_NegativeOffset_ClampsToZero()
        => SourceText.FromString("abc").GetLinePosition(-1).Should().Be(new LinePosition(0, 0));

    // ─── GetOffset ────────────────────────────────────────────────────

    [Fact]
    public void GetOffset_SimpleCase()
    {
        // "func main() {\n print();\n}\n" — line starts: [0, 14, 24, 26]
        var src = SourceText.FromString("func main() {\n print();\n}\n");

        src.GetOffset(new LinePosition(0, 0)).Should().Be(0);   // 'f' (line 0 char 0)
        src.GetOffset(new LinePosition(0, 12)).Should().Be(12); // '{' (line 0 char 12)
        src.GetOffset(new LinePosition(1, 0)).Should().Be(14);  // ' ' at start of line 1
        src.GetOffset(new LinePosition(2, 0)).Should().Be(24);  // '}' at start of line 2
        src.GetOffset(new LinePosition(3, 0)).Should().Be(26);  // trailing line start (beyond text)
    }

    [Fact]
    public void GetOffset_BeyondEnd_ClampsToTextLength()
        => SourceText.FromString("abc").GetOffset(new LinePosition(5, 100)).Should().Be(3);

    [Fact]
    public void GetOffset_NegativeLineOrCharacter_ClampsToZero()
        => SourceText.FromString("abc").GetOffset(new LinePosition(-1, -1)).Should().Be(0);

    // ─── Round-trip: valid positions only ─────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("a\n")]
    [InlineData("a\nb\nc\n")]
    [InlineData("func main() {\n print();\n}\n")]
    public void RoundTrip_GetLinePositionThenOffset(string text)
    {
        var src = SourceText.FromString(text);

        // Every offset from 0 to Length must round-trip
        for (int i = 0; i <= src.Length; i++)
        {
            var pos = src.GetLinePosition(i);
            var backToOffset = src.GetOffset(pos);
            backToOffset.Should().Be(i, $"Round-trip failed at offset {i}");
        }
    }


    [Theory]
    [InlineData("a\r\nb\r\nc\r\n")]
    [InlineData("\r\n")]
    public void RoundTrip_CRNL(string text)
    {
        var src = SourceText.FromString(text);

        for (int i = 0; i <= src.Length; i++)
        {
            var pos = src.GetLinePosition(i);
            var backToOffset = src.GetOffset(pos);
            backToOffset.Should().Be(i, $"Round-trip failed at offset {i}");
        }
    }

    // ─── Unicode (BMP) ────────────────────────────────────────────────

    [Fact]
    public void GetLinePosition_UnicodeBMP()
    {
        // "café\ntest" — 'é' is ONE UTF-16 code unit, length = 9
        var src = SourceText.FromString("café\ntest");
        src.Length.Should().Be(9);

        src.GetLinePosition(4).Should().Be(new LinePosition(0, 4));   // 'é' (line 0 char 4)
        src.GetLinePosition(5).Should().Be(new LinePosition(1, 0));   // 't' (line 1 char 0)

        for (int i = 0; i <= src.Length; i++)
        {
            var pos = src.GetLinePosition(i);
            src.GetOffset(pos).Should().Be(i, $"Round-trip failed at offset {i}");
        }
    }

    [Fact]
    public void GetLinePosition_UnicodeBMP_SingleLine()
    {
        var src = SourceText.FromString("café"); // c,a,f,é — 4 chars
        src.Length.Should().Be(4);
        src.LineCount.Should().Be(1);

        src.GetLinePosition(3).Should().Be(new LinePosition(0, 3)); // 'é' (char 3)
    }

    // ─── LinePositionSpan ─────────────────────────────────────────────

    [Fact]
    public void TextLocation_GetLinePositionSpan_Correct()
    {
        // "abc\ndef\nghi\n" — line starts: [0, 4, 8, 12], length=12
        var src = SourceText.FromString("abc\ndef\nghi\n");

        var loc = new TextLocation(src, new TextSpan(4, 3)); // "def", start at 'd'
        var span = loc.GetLinePositionSpan();

        span.Start.Should().Be(new LinePosition(1, 0)); // 'd' (line 1 char 0)
        span.End.Line.Should().Be(1);
    }

    [Fact]
    public void TextLocation_GetLinePositionSpan_EmptyText()
    {
        var loc = new TextLocation(default, new TextSpan(0, 0));
        var span = loc.GetLinePositionSpan();
        span.Start.Should().Be(default(LinePosition));
        span.End.Should().Be(default(LinePosition));
    }

    // ─── Helper properties on TextLocation ────────────────────────────

    [Fact]
    public void TextLocation_LineNumberAndCharacterNumber()
    {
        var src = SourceText.FromString("abc\ndef\nghi\n");

        var loc = new TextLocation(src, new TextSpan(4, 3)); // "def"
        loc.LineNumber.Should().Be(1);   // line 1
        loc.CharacterNumber.Should().Be(0); // char 0 of line 1

        var loc2 = new TextLocation(src, new TextSpan(6, 1)); // "f", offset 6 → line 1 char 2
        loc2.LineNumber.Should().Be(1);
        loc2.CharacterNumber.Should().Be(2);
    }

    [Fact]
    public void TextLocation_GetText_ReturnsSourceText()
    {
        var src = SourceText.FromString("abc\ndef\nghi\n");
        var loc = new TextLocation(src, new TextSpan(4, 3)); // "def"
        loc.GetText().Should().Be("def");
    }

    [Fact]
    public void TextLocation_GetText_NullSourceText_ReturnsEmpty()
    {
        var loc = new TextLocation(null, new TextSpan(0, 0));
        loc.GetText().Should().Be("");
    }

    // ─── LinePosition equality ────────────────────────────────────────

    [Fact]
    public void LinePosition_Equality()
    {
        var a = new LinePosition(0, 0);
        var b = new LinePosition(0, 0);
        var c = new LinePosition(1, 0);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    // ─── LinePositionSpan validity ────────────────────────────────────

    [Theory]
    [InlineData(0, 0, 1, 0, true)]
    [InlineData(0, 0, 0, 5, true)]
    [InlineData(0, 5, 0, 5, true)]
    [InlineData(0, 5, 0, 3, false)]
    public void LinePositionSpan_IsValid(int sl, int sc, int el, int ec, bool expected)
    {
        var span = new LinePositionSpan(new LinePosition(sl, sc), new LinePosition(el, ec));
        span.IsValid.Should().Be(expected);
    }

    [Fact]
    public void LinePositionSpan_PositionAccess()
    {
        var span = new LinePositionSpan(new LinePosition(1, 0), new LinePosition(2, 0));
        span.Start.Should().Be(new LinePosition(1, 0));
        span.End.Should().Be(new LinePosition(2, 0));
    }

    // ─── GetOffset for \r\n line ──────────────────────────────────────

    [Fact]
    public void GetOffset_CRNL_LineBreaks()
    {
        var src = SourceText.FromString("a\r\nb");
        src.GetOffset(new LinePosition(0, 0)).Should().Be(0);  // 'a'
        src.GetOffset(new LinePosition(0, 1)).Should().Be(1);  // '\r'
        src.GetOffset(new LinePosition(0, 2)).Should().Be(2);  // '\n'
        src.GetOffset(new LinePosition(1, 0)).Should().Be(3);  // 'b'
    }

    // ─── Lone \r edge case ────────────────────────────────────────────

    [Fact]
    public void GetLinePosition_LoneCR_SingleChar()
    {
        var src = SourceText.FromString("\r");
        src.LineCount.Should().Be(2);

        src.GetLinePosition(0).Should().Be(new LinePosition(0, 0)); // '\r' on line 0
        // offset 1 == Length: now correctly resolves to the trailing empty line
        src.GetLinePosition(1).Should().Be(new LinePosition(1, 0)); // eof → line 1 (empty after break)
    }

    // ─── GetEndLinePosition crosses lines ─────────────────────────────

    [Fact]
    public void TextLocation_GetEndLinePosition_CrossesLines()
    {
        var src = SourceText.FromString("abc\ndef\nghi\n");
        // length=12, line starts: [0, 4, 8, 12]
        // Span (4, 3) → "def", end at offset 7 which is 'f' on line 1 (char index 3)
        var loc = new TextLocation(src, new TextSpan(4, 3));
        var endPos = loc.GetEndLinePosition();
        endPos.Line.Should().Be(1);
    }

    [Fact]
    public void TextLocation_GetEndLinePosition_EndAtEOF()
    {
        var src = SourceText.FromString("abc\ndef\nghi\n");
        // length=12, LineCount=4. Span (0, 12) → entire text, end at EOF offset 12
        var loc = new TextLocation(src, new TextSpan(0, 12));
        var endPos = loc.GetEndLinePosition();
        endPos.Line.Should().Be(3); // trailing empty line (char 0 after line start 12)
    }

    [Fact]
    public void GetLinePosition_EOF_RoundTrip_AllTexts()
    {
        foreach (var text in new[] { "", "hello", "a\n", "a\nb\nc\n", "\r\n", "func main() {\n print();\n}\n" })
        {
            var src = SourceText.FromString(text);

            // Every offset from 0 to Length must round-trip through GetLinePosition → GetOffset
            for (int i = 0; i <= src.Length; i++)
            {
                var pos = src.GetLinePosition(i);
                var back = src.GetOffset(pos);
                back.Should().Be(i, $"Round-trip at offset {i} failed for text: {Escape(text)}");
            }
        }
    }

    [Fact]
    public void GetLinePosition_EOF_Position_MatchesTrailingLine()
    {
        var src = SourceText.FromString("a\nb\n"); // length=4, line starts: [0, 2, 4], LineCount=3
        // EOF at offset 4 → should resolve to line 2 char 0 (trailing empty line)
        var eofPos = src.GetLinePosition(src.Length);
        eofPos.Line.Should().Be(2);
        eofPos.Character.Should().Be(0);

        // Round-trip: this position's offset must be Length
        src.GetOffset(eofPos).Should().Be(4);
    }
    private static string Escape(string text)
        => text.Replace("\r", "\\r").Replace("\n", "\\n");
}
