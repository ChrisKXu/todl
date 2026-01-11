using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BinaryExpressionTests
{
    [Fact]
    public void TestParseBinaryExpressionBasic()
    {
        var binaryExpression = TestUtils.ParseExpression<BinaryExpression>("1 + 2 + 3");

        binaryExpression.Left.As<BinaryExpression>().Invoking(left =>
        {
            left.Left.As<LiteralExpression>().Text.Should().Be("1");
            left.Operator.Text.Should().Be("+");
            left.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
            left.Right.As<LiteralExpression>().Text.Should().Be("2");
        }).Should().NotThrow();

        binaryExpression.Operator.Text.Should().Be("+");
        binaryExpression.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
        binaryExpression.Right.As<LiteralExpression>().Text.Should().Be("3");
    }

    [Fact]
    public void TestParseBinaryExpressionWithPrecedence()
    {
        var binaryExpression = TestUtils.ParseExpression<BinaryExpression>("1 + 2 * 3 - 4");

        var left = binaryExpression.Left.As<BinaryExpression>();
        left.Left.As<LiteralExpression>().Text.Should().Be("1");
        left.Operator.Text.Should().Be("+");
        left.Operator.Kind.Should().Be(SyntaxKind.PlusToken);

        left.Right.As<BinaryExpression>().Invoking(multiplication =>
        {
            multiplication.Left.As<LiteralExpression>().Text.Should().Be("2");
            multiplication.Operator.Text.Should().Be("*");
            multiplication.Operator.Kind.Should().Be(SyntaxKind.StarToken);
            multiplication.Right.As<LiteralExpression>().Text.Should().Be("3");

            binaryExpression.Operator.Text.Should().Be("-");
            binaryExpression.Operator.Kind.Should().Be(SyntaxKind.MinusToken);
            binaryExpression.Right.As<LiteralExpression>().Text.Should().Be("4");
        }).Should().NotThrow();
    }

    [Fact]
    public void TestParseBinaryExpressionWithPrecedence2()
    {
        var binaryExpression = TestUtils.ParseExpression<BinaryExpression>("1 + 2 * 3 <= 4");

        var left = binaryExpression.Left.As<BinaryExpression>();
        left.Left.As<LiteralExpression>().Text.Should().Be("1");
        left.Operator.Text.Should().Be("+");
        left.Operator.Kind.Should().Be(SyntaxKind.PlusToken);

        left.Right.As<BinaryExpression>().Invoking(multiplication =>
        {
            multiplication.Left.As<LiteralExpression>().Text.Should().Be("2");
            multiplication.Operator.Text.Should().Be("*");
            multiplication.Operator.Kind.Should().Be(SyntaxKind.StarToken);
            multiplication.Right.As<LiteralExpression>().Text.Should().Be("3");

            binaryExpression.Operator.Text.Should().Be("<=");
            binaryExpression.Operator.Kind.Should().Be(SyntaxKind.LessThanOrEqualsToken);
            binaryExpression.Right.As<LiteralExpression>().Text.Should().Be("4");
        }).Should().NotThrow();
    }

    [Fact]
    public void TestParseBinaryExpressionWithParenthesis()
    {
        var binaryExpression = TestUtils.ParseExpression<BinaryExpression>("(1 + 2) * 3 - 4");

        binaryExpression.Left.As<BinaryExpression>().Invoking(multiplication =>
        {
            multiplication.Left.As<ParethesizedExpression>().InnerExpression.As<BinaryExpression>().Invoking(inner =>
            {
                inner.Left.As<LiteralExpression>().Text.Should().Be("1");
                inner.Operator.Text.Should().Be("+");
                inner.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
                inner.Right.As<LiteralExpression>().Text.Should().Be("2");
            }).Should().NotThrow();

            multiplication.Operator.Text.Should().Be("*");
            multiplication.Operator.Kind.Should().Be(SyntaxKind.StarToken);
            multiplication.Right.As<LiteralExpression>().Text.Should().Be("3");
        }).Should().NotThrow();

        binaryExpression.Operator.Text.Should().Be("-");
        binaryExpression.Operator.Kind.Should().Be(SyntaxKind.MinusToken);
        binaryExpression.Right.As<LiteralExpression>().Text.Should().Be("4");
    }

    [Fact]
    public void TestParseBinaryExpressionWithEquality()
    {
        var binaryExpression = TestUtils.ParseExpression<BinaryExpression>("3 == 1 + 2");

        binaryExpression.Left.Text.Should().Be("3");
        binaryExpression.Operator.Text.Should().Be("==");
        binaryExpression.Operator.Kind.Should().Be(SyntaxKind.EqualsEqualsToken);

        binaryExpression.Right.As<BinaryExpression>().Invoking(right =>
        {
            right.Left.As<LiteralExpression>().Text.Should().Be("1");
            right.Operator.Text.Should().Be("+");
            right.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
            right.Right.As<LiteralExpression>().Text.Should().Be("2");
        }).Should().NotThrow();
    }

    [Fact]
    public void TestParseBinaryExpressionWithInequality()
    {
        var binaryExpression = TestUtils.ParseExpression<BinaryExpression>("5 != 1 + 2");

        binaryExpression.Left.As<LiteralExpression>().Text.Should().Be("5");
        binaryExpression.Operator.Text.Should().Be("!=");
        binaryExpression.Operator.Kind.Should().Be(SyntaxKind.BangEqualsToken);

        binaryExpression.Right.As<BinaryExpression>().Invoking(right =>
        {
            right.Left.As<LiteralExpression>().Text.Should().Be("1");
            right.Operator.Text.Should().Be("+");
            right.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
            right.Right.As<LiteralExpression>().Text.Should().Be("2");
        }).Should().NotThrow();
    }

    [Fact]
    public void TestParseBinaryExpressionWithNameAndUnaryExpression()
    {
        var binaryExpression = TestUtils.ParseExpression<BinaryExpression>("(-a + 2) * 3 + 4");

        binaryExpression.Left.As<BinaryExpression>().Invoking(multiplication =>
        {
            multiplication.Left.As<ParethesizedExpression>().InnerExpression.As<BinaryExpression>().Invoking(inner =>
            {
                inner.Left.As<UnaryExpression>().Invoking(unaryExpression =>
                {
                    unaryExpression.Operator.Text.Should().Be("-");
                    unaryExpression.Operator.Kind.Should().Be(SyntaxKind.MinusToken);
                    unaryExpression.Operand.As<SimpleNameExpression>().Text.Should().Be("a");
                }).Should().NotThrow();

                inner.Operator.Text.Should().Be("+");
                inner.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
                inner.Right.As<LiteralExpression>().Text.Should().Be("2");
            }).Should().NotThrow();

            multiplication.Operator.Text.Should().Be("*");
            multiplication.Operator.Kind.Should().Be(SyntaxKind.StarToken);
            multiplication.Right.As<LiteralExpression>().Text.Should().Be("3");
        }).Should().NotThrow();

        binaryExpression.Operator.Text.Should().Be("+");
        binaryExpression.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
        binaryExpression.Right.As<LiteralExpression>().Text.Should().Be("4");
    }

    [Theory]
    [InlineData("a & b", "&", SyntaxKind.AmpersandToken)]
    [InlineData("a | b", "|", SyntaxKind.PipeToken)]
    [InlineData("a && b", "&&", SyntaxKind.AmpersandAmpersandToken)]
    [InlineData("a || b", "||", SyntaxKind.PipePipeToken)]
    public void TestParseBinaryExpressionWithBitwiseAndLogicalOperators(string input, string expectedOperatorText, SyntaxKind expectedKind)
    {
        var binaryExpression = TestUtils.ParseExpression<BinaryExpression>(input);

        binaryExpression.Should().NotBeNull();
        binaryExpression.Left.As<SimpleNameExpression>().Text.Should().Be("a");
        binaryExpression.Operator.Text.Should().Be(expectedOperatorText);
        binaryExpression.Operator.Kind.Should().Be(expectedKind);
        binaryExpression.Right.As<SimpleNameExpression>().Text.Should().Be("b");
    }

    [Theory]
    [InlineData("a < b", "<", SyntaxKind.LessThanToken)]
    [InlineData("a > b", ">", SyntaxKind.GreaterThanToken)]
    [InlineData("a <= b", "<=", SyntaxKind.LessThanOrEqualsToken)]
    [InlineData("a >= b", ">=", SyntaxKind.GreaterThanOrEqualsToken)]
    public void TestParseBinaryExpressionWithComparisonOperators(string input, string expectedOperatorText, SyntaxKind expectedKind)
    {
        var binaryExpression = TestUtils.ParseExpression<BinaryExpression>(input);

        binaryExpression.Should().NotBeNull();
        binaryExpression.Left.As<SimpleNameExpression>().Text.Should().Be("a");
        binaryExpression.Operator.Text.Should().Be(expectedOperatorText);
        binaryExpression.Operator.Kind.Should().Be(expectedKind);
        binaryExpression.Right.As<SimpleNameExpression>().Text.Should().Be("b");
    }

    [Fact]
    public void TestParseBinaryExpressionWithDivision()
    {
        var binaryExpression = TestUtils.ParseExpression<BinaryExpression>("10 / 2");

        binaryExpression.Should().NotBeNull();
        binaryExpression.Left.As<LiteralExpression>().Text.Should().Be("10");
        binaryExpression.Operator.Text.Should().Be("/");
        binaryExpression.Operator.Kind.Should().Be(SyntaxKind.SlashToken);
        binaryExpression.Right.As<LiteralExpression>().Text.Should().Be("2");
    }

    [Fact]
    public void TestComplexBooleanExpressionPrecedence()
    {
        // a && b || c && d should be parsed as ((a && b) || (c && d))
        // because && has higher precedence than ||
        var binaryExpression = TestUtils.ParseExpression<BinaryExpression>("a && b || c && d");

        binaryExpression.Should().NotBeNull();
        binaryExpression.Operator.Kind.Should().Be(SyntaxKind.PipePipeToken);

        var left = binaryExpression.Left.As<BinaryExpression>();
        left.Operator.Kind.Should().Be(SyntaxKind.AmpersandAmpersandToken);
        left.Left.As<SimpleNameExpression>().Text.Should().Be("a");
        left.Right.As<SimpleNameExpression>().Text.Should().Be("b");

        var right = binaryExpression.Right.As<BinaryExpression>();
        right.Operator.Kind.Should().Be(SyntaxKind.AmpersandAmpersandToken);
        right.Left.As<SimpleNameExpression>().Text.Should().Be("c");
        right.Right.As<SimpleNameExpression>().Text.Should().Be("d");
    }

    [Fact]
    public void TestBitwiseOperatorPrecedence()
    {
        // a | b & c should be parsed as (a | (b & c))
        // because & has higher precedence than |
        var binaryExpression = TestUtils.ParseExpression<BinaryExpression>("a | b & c");

        binaryExpression.Should().NotBeNull();
        binaryExpression.Operator.Kind.Should().Be(SyntaxKind.PipeToken);
        binaryExpression.Left.As<SimpleNameExpression>().Text.Should().Be("a");

        var right = binaryExpression.Right.As<BinaryExpression>();
        right.Operator.Kind.Should().Be(SyntaxKind.AmpersandToken);
        right.Left.As<SimpleNameExpression>().Text.Should().Be("b");
        right.Right.As<SimpleNameExpression>().Text.Should().Be("c");
    }

    [Fact]
    public void TestMixedArithmeticAndComparisonPrecedence()
    {
        // a + b < c * d should be parsed as ((a + b) < (c * d))
        var binaryExpression = TestUtils.ParseExpression<BinaryExpression>("a + b < c * d");

        binaryExpression.Should().NotBeNull();
        binaryExpression.Operator.Kind.Should().Be(SyntaxKind.LessThanToken);

        var left = binaryExpression.Left.As<BinaryExpression>();
        left.Operator.Kind.Should().Be(SyntaxKind.PlusToken);

        var right = binaryExpression.Right.As<BinaryExpression>();
        right.Operator.Kind.Should().Be(SyntaxKind.StarToken);
    }
}
