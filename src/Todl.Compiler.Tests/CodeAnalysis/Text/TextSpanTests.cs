using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class TextSpanTests
{
    [Fact]
    public void TextSpansWithEqualCoordinatesAreEqualRegardlessOfOrigin()
    {
        // TextSpan is a pure (Start, Length) coordinate: spans carved out of completely
        // different SourceText instances (old vs. new text) must still compare equal
        // as long as Start/Length match.
        var oldSourceText = SourceText.FromString("let a = 1;");
        var newSourceText = SourceText.FromString("let a = 2; extra");

        var spanFromOld = new TextSpan(4, 1);
        var spanFromNew = new TextSpan(4, 1);

        spanFromOld.Should().Be(spanFromNew);
        (spanFromOld == spanFromNew).Should().BeTrue();
        spanFromOld.GetHashCode().Should().Be(spanFromNew.GetHashCode());

        // The two spans resolve to different text against their respective sources,
        // which is expected now that resolution is SourceText's responsibility, not TextSpan's.
        oldSourceText.ToString(spanFromOld).Should().Be("a");
        newSourceText.ToString(spanFromNew).Should().Be("a");
    }

    [Fact]
    public void TextSpansWithDifferentCoordinatesAreNotEqual()
    {
        new TextSpan(0, 3).Should().NotBe(new TextSpan(0, 4));
        new TextSpan(0, 3).Should().NotBe(new TextSpan(1, 3));
    }

    [Theory]
    [InlineData(0, 5, 5)]
    [InlineData(3, 10, 7)]
    public void EndIsStartPlusLength(int start, int end, int expectedLength)
    {
        var span = TextSpan.FromBounds(start, end);
        span.Start.Should().Be(start);
        span.Length.Should().Be(expectedLength);
        span.End.Should().Be(end);
    }
}
