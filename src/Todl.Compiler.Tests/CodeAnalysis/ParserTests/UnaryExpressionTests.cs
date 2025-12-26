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

        unaryExpression.Operand.Should().BeOfType<NameExpression>();
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
}
