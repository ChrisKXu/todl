using System;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class SourceTextWithChangesTests
{
    // ─── Replace / insert / delete ───────────────────────────────────

    [Fact]
    public void WithChange_Replace_SplicesNewText()
    {
        var src = SourceText.FromString("let x = 1;");
        var result = src.WithChange(new TextSpan(8, 1), "42");

        result.Text.Should().Be("let x = 42;");
        result.Version.Should().Be(1);
    }

    [Fact]
    public void WithChange_Insert_EmptySpanInsertsAtPosition()
    {
        var src = SourceText.FromString("ac");
        var result = src.WithChange(new TextSpan(1, 0), "b");

        result.Text.Should().Be("abc");
    }

    [Fact]
    public void WithChange_Delete_EmptyNewTextRemovesSpan()
    {
        var src = SourceText.FromString("abc");
        var result = src.WithChange(new TextSpan(1, 1), "");

        result.Text.Should().Be("ac");
    }

    // ─── Multi-edit ordering ──────────────────────────────────────────

    [Fact]
    public void WithChanges_MultipleOrderedEdits_AppliedLeftToRight()
    {
        var src = SourceText.FromString("abcdef");
        var result = src.WithChanges(new[]
        {
            new TextChange(new TextSpan(0, 1), "A"),
            new TextChange(new TextSpan(4, 1), "E"),
        });

        result.Text.Should().Be("AbcdEf");
    }

    [Fact]
    public void WithChanges_UnsortedInput_IsDefensivelySorted()
    {
        var src = SourceText.FromString("abcdef");
        var result = src.WithChanges(new[]
        {
            new TextChange(new TextSpan(4, 1), "E"),
            new TextChange(new TextSpan(0, 1), "A"),
        });

        result.Text.Should().Be("AbcdEf");
    }

    [Fact]
    public void WithChanges_TouchingSpans_AreNotOverlapping()
    {
        // second span starts exactly where the first ends - adjacent, not overlapping.
        var src = SourceText.FromString("abcdef");
        var result = src.WithChanges(new[]
        {
            new TextChange(new TextSpan(0, 2), "XX"),
            new TextChange(new TextSpan(2, 2), "YY"),
        });

        result.Text.Should().Be("XXYYef");
    }

    [Fact]
    public void WithChanges_ZeroLengthInsertsAtSameOffset_AppliedInInputOrder()
    {
        // Both spans are empty at the same Start, so the overlap check can't order them -
        // the stable sort preserves input order. This locks in that documented tie-break.
        var src = SourceText.FromString("ac");
        var result = src.WithChanges(new[]
        {
            new TextChange(new TextSpan(1, 0), "b"),
            new TextChange(new TextSpan(1, 0), "B"),
        });

        result.Text.Should().Be("abBc");
    }

    [Fact]
    public void WithChanges_PreservesFilePath()
    {
        var src = new SourceText { FilePath = "test.todl", Text = "abc" };
        var result = src.WithChange(new TextSpan(0, 1), "X");

        result.FilePath.Should().Be("test.todl");
    }

    // ─── Validation ───────────────────────────────────────────────────

    [Fact]
    public void WithChanges_OverlappingSpans_Throws()
    {
        var src = SourceText.FromString("abcdef");
        Action act = () => src.WithChanges(new[]
        {
            new TextChange(new TextSpan(0, 3), "X"),
            new TextChange(new TextSpan(2, 3), "Y"),
        });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithChanges_SpanBeyondLength_Throws()
    {
        var src = SourceText.FromString("abc");
        Action act = () => src.WithChange(new TextSpan(2, 5), "x");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithChanges_NegativeSpanStart_Throws()
    {
        var src = SourceText.FromString("abc");
        Action act = () => src.WithChange(new TextSpan(-1, 1), "x");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithChanges_NullChanges_Throws()
    {
        var src = SourceText.FromString("abc");
        Action act = () => src.WithChanges(null);

        act.Should().Throw<ArgumentNullException>();
    }

    // ─── No-op ──────────────────────────────────────────────────────

    [Fact]
    public void WithChanges_EmptyChangeList_ReturnsSameInstance()
    {
        var src = SourceText.FromString("abc");
        src.WithChanges(Array.Empty<TextChange>()).Should().BeSameAs(src);
    }

    // ─── Version ────────────────────────────────────────────────────

    [Fact]
    public void Version_StartsAtZero_IncrementsPerWithChanges()
    {
        var src = SourceText.FromString("abc");
        src.Version.Should().Be(0);

        var v1 = src.WithChange(new TextSpan(0, 1), "A");
        var v2 = v1.WithChange(new TextSpan(0, 1), "B");

        v1.Version.Should().Be(1);
        v2.Version.Should().Be(2);
    }

    // ─── Property: length == oldLength + Σdelta ──────────────────────

    [Theory]
    [InlineData("abcdef", 0, 1, "AB")]     // replace, grows
    [InlineData("abcdef", 0, 3, "")]       // delete
    [InlineData("abcdef", 3, 0, "XYZ")]    // insert
    public void WithChange_LengthMatchesDelta(string text, int start, int length, string newText)
    {
        var src = SourceText.FromString(text);
        var change = new TextChange(new TextSpan(start, length), newText);
        var result = src.WithChange(change.Span, change.NewText);

        result.Length.Should().Be(src.Length + change.Delta);
    }

    // ─── TextChange.Delta ─────────────────────────────────────────────

    [Fact]
    public void TextChange_Delta_IsNewTextLengthMinusSpanLength()
    {
        new TextChange(new TextSpan(0, 3), "ab").Delta.Should().Be(-1);
        new TextChange(new TextSpan(0, 0), "abc").Delta.Should().Be(3);
        new TextChange(new TextSpan(0, 2), "xy").Delta.Should().Be(0);
    }

    // ─── GetChangeRanges stub ─────────────────────────────────────────

    [Fact]
    public void GetChangeRanges_DifferentText_ReturnsSingleFullSpanRange()
    {
        var oldText = SourceText.FromString("let x = 1;");
        var newText = oldText.WithChange(new TextSpan(8, 1), "42");

        var ranges = newText.GetChangeRanges(oldText);

        ranges.Should().ContainSingle();
        ranges[0].Span.Should().Be(new TextSpan(0, oldText.Length));
        ranges[0].NewLength.Should().Be(newText.Length);
    }

    [Fact]
    public void GetChangeRanges_IdenticalText_ReturnsEmpty()
    {
        var oldText = SourceText.FromString("abc");
        var sameText = SourceText.FromString("abc");

        sameText.GetChangeRanges(oldText).Should().BeEmpty();
    }

    [Fact]
    public void GetChangeRanges_NullOldText_Throws()
    {
        var src = SourceText.FromString("abc");
        Action act = () => src.GetChangeRanges(null);

        act.Should().Throw<ArgumentNullException>();
    }

    // ─── Round-trip with line/offset mapping (depends on 03) ─────────

    [Fact]
    public void WithChange_RoundTripsWithLinePositionOffsets()
    {
        // "func main() {\n print();\n}\n" — replace "print" (line 1, chars 1-6) with "log"
        var src = SourceText.FromString("func main() {\n print();\n}\n");
        var start = src.GetOffset(new LinePosition(1, 1));
        var end = src.GetOffset(new LinePosition(1, 6));

        var result = src.WithChange(TextSpan.FromBounds(start, end), "log");

        result.Text.Should().Be("func main() {\n log();\n}\n");
    }
}
