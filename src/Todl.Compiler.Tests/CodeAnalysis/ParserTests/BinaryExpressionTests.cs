using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed partial class ParserTests
{
    [Fact]
    public void TestParseBinaryExpressionBasic()
    {
        var binaryExpression = ParseExpression<BinaryExpression>("1 + 2 + 3");

        binaryExpression.Left.As<BinaryExpression>().Invoking(left =>
        {
            left.Left.As<LiteralExpression>().Text.Should().Be("1");
            left.Operator.Text.Should().Be("+");
            left.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
            left.Right.As<LiteralExpression>().Text.Should().Be("2");
        });

        binaryExpression.Operator.Text.Should().Be("+");
        binaryExpression.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
        binaryExpression.Right.As<LiteralExpression>().Text.Should().Be("3");
    }

    [Fact]
    public void TestParseBinaryExpressionWithPrecedence()
    {
        var binaryExpression = ParseExpression<BinaryExpression>("1 + 2 * 3 - 4");

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
        });
    }

    [Fact]
    public void TestParseBinaryExpressionWithParenthesis()
    {
        var binaryExpression = ParseExpression<BinaryExpression>("(1 + 2) * 3 - 4");

        binaryExpression.Left.As<BinaryExpression>().Invoking(multiplication =>
        {
            multiplication.Left.As<ParethesizedExpression>().InnerExpression.As<BinaryExpression>().Invoking(inner =>
            {
                inner.Left.As<LiteralExpression>().Text.Should().Be("1");
                inner.Operator.Text.Should().Be("+");
                inner.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
                inner.Right.As<LiteralExpression>().Text.Should().Be("2");
            });

            multiplication.Operator.Text.Should().Be("*");
            multiplication.Operator.Kind.Should().Be(SyntaxKind.StarToken);
            multiplication.As<LiteralExpression>().Text.Should().Be("4");
        });

        binaryExpression.Operator.Text.Should().Be("-");
        binaryExpression.Operator.Kind.Should().Be(SyntaxKind.MinusToken);
        binaryExpression.Right.As<LiteralExpression>().Text.Should().Be("4");
    }

    [Fact]
    public void TestParseBinaryExpressionWithEquality()
    {
        var binaryExpression = ParseExpression<BinaryExpression>("3 == 1 + 2");

        binaryExpression.Left.Text.Should().Be("3");
        binaryExpression.Operator.Text.Should().Be("==");
        binaryExpression.Operator.Kind.Should().Be(SyntaxKind.EqualsEqualsToken);

        binaryExpression.Right.As<BinaryExpression>().Invoking(right =>
        {
            right.Left.As<LiteralExpression>().Text.Should().Be("1");
            right.Operator.Text.Should().Be("+");
            right.Operator.Kind.Should().Be(SyntaxKind.PlusPlusToken);
            right.Right.As<LiteralExpression>().Text.Should().Be("2");
        });
    }

    [Fact]
    public void TestParseBinaryExpressionWithInequality()
    {
        var binaryExpression = ParseExpression<BinaryExpression>("5 != 1 + 2");

        binaryExpression.Left.As<LiteralExpression>().Text.Should().Be("5");
        binaryExpression.Operator.Text.Should().Be("!=");
        binaryExpression.Operator.Kind.Should().Be(SyntaxKind.BangEqualsToken);

        binaryExpression.Right.As<BinaryExpression>().Invoking(right =>
        {
            right.Left.As<LiteralExpression>().Text.Should().Be("1");
            right.Operator.Text.Should().Be("+");
            right.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
            right.Right.As<LiteralExpression>().Text.Should().Be("2");
        });
    }

    [Fact]
    public void TestParseBinaryExpressionWithNameAndUnaryExpression()
    {
        var binaryExpression = ParseExpression<BinaryExpression>("(++a + 2) * 3 + 4");

        binaryExpression.Left.As<BinaryExpression>().Invoking(multiplication =>
        {
            multiplication.Left.As<ParethesizedExpression>().InnerExpression.As<BinaryExpression>().Invoking(inner =>
            {
                inner.Left.As<UnaryExpression>().Invoking(unaryExpression =>
                {
                    unaryExpression.Operator.Text.Should().Be("++");
                    unaryExpression.Operator.Kind.Should().Be(SyntaxKind.PlusPlusToken);
                    unaryExpression.Operand.As<NameExpression>().Text.Should().Be("a");
                    unaryExpression.Trailing.Should().Be(false);
                });

                inner.Operator.Text.Should().Be("+");
                inner.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
                inner.Right.As<LiteralExpression>().Text.Should().Be("2");
            });

            multiplication.Operator.Text.Should().Be("*");
            multiplication.Operator.Kind.Should().Be(SyntaxKind.StarToken);
            multiplication.Right.As<LiteralExpression>().Text.Should().Be("3");
        });

        binaryExpression.Operator.Text.Should().Be("+");
        binaryExpression.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
        binaryExpression.Right.As<LiteralExpression>().Text.Should().Be("4");
    }

    [Fact]
    public void TestParseBinaryExpressionWithNameAndTrailingUnaryExpression()
    {
        var binaryExpression = ParseExpression<BinaryExpression>("(a++ + 2) * 3");

        binaryExpression.Left.As<ParethesizedExpression>().InnerExpression.As<BinaryExpression>().Invoking(inner =>
        {
            inner.Left.As<UnaryExpression>().Invoking(unaryExpression =>
            {
                unaryExpression.Operand.As<NameExpression>().QualifiedName.Should().Be("a");
                unaryExpression.Operator.Text.Should().Be("++");
                unaryExpression.Operator.Kind.Should().Be(SyntaxKind.PlusPlusToken);
                unaryExpression.Trailing.Should().Be(true);
            });

            inner.Operator.Text.Should().Be("+");
            inner.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
            inner.Right.As<LiteralExpression>().Text.Should().Be("2");
        });

        binaryExpression.Operator.Text.Should().Be("*");
        binaryExpression.Operator.Kind.Should().Be(SyntaxKind.StarToken);
        binaryExpression.Right.As<LiteralExpression>().Text.Should().Be("3");
    }
}
