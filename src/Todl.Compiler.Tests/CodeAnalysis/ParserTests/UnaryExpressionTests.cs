using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class UnaryExpressionTests
{
    [Theory]
    [InlineData("+1", SyntaxKind.PlusToken)]
    [InlineData("-1", SyntaxKind.MinusToken)]
    public void TestParingUnaryExpressionWithConstants(string input, SyntaxKind expectedOperatorKind)
    {
        var unaryExpression = TestUtils.ParseExpression<UnaryExpression>(input);

        unaryExpression.Should().NotBeNull();
        unaryExpression.Operator.Kind.Should().Be(expectedOperatorKind);

        unaryExpression.Operand.Should().BeOfType<LiteralExpression>();
    }

    [Theory]
    [InlineData("+a", SyntaxKind.PlusToken)]
    [InlineData("-a", SyntaxKind.MinusToken)]
    [InlineData("!a", SyntaxKind.BangToken)]
    [InlineData("~a", SyntaxKind.TildeToken)]
    public void TestParsingUnaryExpression(string input, SyntaxKind expectedOperatorKind)
    {
        var unaryExpression = TestUtils.ParseExpression<UnaryExpression>(input);

        unaryExpression.Should().NotBeNull();
        unaryExpression.Operator.Kind.Should().Be(expectedOperatorKind);

        unaryExpression.Operand.Should().BeOfType<SimpleNameExpression>();
    }

    [Theory]
    [InlineData("+TestClass.PublicStaticIntProperty", SyntaxKind.PlusToken)]
    [InlineData("-TestClass.PublicStaticIntProperty", SyntaxKind.MinusToken)]
    [InlineData("!TestClass.PublicStaticBoolProperty", SyntaxKind.BangToken)]
    [InlineData("~TestClass.PublicStaticIntProperty", SyntaxKind.TildeToken)]
    public void TestParsingUnaryExpressionWithMemberAccessExpression(string input, SyntaxKind expectedOperatorKind)
    {
        var unaryExpression = TestUtils.ParseExpression<UnaryExpression>(input);

        unaryExpression.Should().NotBeNull();
        unaryExpression.Operator.Kind.Should().Be(expectedOperatorKind);

        unaryExpression.Operand.Should().BeOfType<MemberAccessExpression>();
    }

    [Fact]
    public void TestParsingUnaryWithParenthesizedExpression()
    {
        var unaryExpression = TestUtils.ParseExpression<UnaryExpression>("-(a + b)");

        unaryExpression.Should().NotBeNull();
        unaryExpression.Operator.Kind.Should().Be(SyntaxKind.MinusToken);

        var parenExpr = unaryExpression.Operand.As<ParethesizedExpression>();
        parenExpr.Should().NotBeNull();
        parenExpr.InnerExpression.Should().BeOfType<BinaryExpression>();
    }

    [Fact]
    public void TestParsingBitwiseNotWithLiteral()
    {
        var unaryExpression = TestUtils.ParseExpression<UnaryExpression>("~255");

        unaryExpression.Should().NotBeNull();
        unaryExpression.Operator.Kind.Should().Be(SyntaxKind.TildeToken);
        unaryExpression.Operand.As<LiteralExpression>().Text.Should().Be("255");
    }
}
