using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class ArrayLiteralExpressionTests
{
    [Fact]
    public void TestParseArrayLiteralExpressionWithOneElement()
    {
        var inputText = "[1]";
        var arrayLiteralExpression = TestUtils.ParseExpression<ArrayLiteralExpression>(inputText);

        arrayLiteralExpression.Elements.Should().HaveCount(1);
        arrayLiteralExpression.Elements[0].GetText().Should().Be("1");
    }

    [Fact]
    public void TestParseArrayLiteralExpressionWithMultipleElements()
    {
        var inputText = "[1, 2, 3]";
        var arrayLiteralExpression = TestUtils.ParseExpression<ArrayLiteralExpression>(inputText);

        arrayLiteralExpression.Elements.Should().HaveCount(3);
        arrayLiteralExpression.Elements[0].GetText().Should().Be("1");
        arrayLiteralExpression.Elements[1].GetText().Should().Be("2");
        arrayLiteralExpression.Elements[2].GetText().Should().Be("3");
    }

    [Fact]
    public void TestParseNestedArrayLiteralExpression()
    {
        var inputText = "[[1, 2], [3, 4, 5]]";
        var arrayLiteralExpression = TestUtils.ParseExpression<ArrayLiteralExpression>(inputText);

        arrayLiteralExpression.Elements.Should().HaveCount(2);
        arrayLiteralExpression.Elements[0].Should().BeOfType<ArrayLiteralExpression>();
        arrayLiteralExpression.Elements[1].As<ArrayLiteralExpression>().Elements.Should().HaveCount(3);
    }

    [Fact]
    public void TestParseArrayLiteralExpressionIsPrimaryExpressionReceiver()
    {
        var inputText = "[1, 2, 3].Length";
        var memberAccessExpression = TestUtils.ParseExpression<MemberAccessExpression>(inputText);

        memberAccessExpression.BaseExpression.Should().BeOfType<ArrayLiteralExpression>();
        memberAccessExpression.MemberIdentifierToken.Text.ToString().Should().Be("Length");
    }
}
