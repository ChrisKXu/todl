using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class UnaryExpressionTests
{
    [Theory]
    [InlineData("+1", SyntaxKind.PlusToken, false)]
    [InlineData("-1", SyntaxKind.MinusToken, false)]
    public void TestParingUnaryExpressionWithConstants(string input, SyntaxKind expectedOperatorKind, bool trailing)
    {
        var unaryExpression = TestUtils.ParseExpression<UnaryExpression>(input);

        unaryExpression.Should().NotBeNull();
        unaryExpression.Operator.Kind.Should().Be(expectedOperatorKind);
        unaryExpression.Trailing.Should().Be(trailing);

        unaryExpression.Operand.Should().BeOfType<LiteralExpression>();
    }

    [Theory]
    [InlineData("+a", SyntaxKind.PlusToken, false)]
    [InlineData("-a", SyntaxKind.MinusToken, false)]
    [InlineData("++a", SyntaxKind.PlusPlusToken, false)]
    [InlineData("--a", SyntaxKind.MinusMinusToken, false)]
    [InlineData("a++", SyntaxKind.PlusPlusToken, true)]
    [InlineData("a--", SyntaxKind.MinusMinusToken, true)]
    [InlineData("!a", SyntaxKind.BangToken, false)]
    [InlineData("~a", SyntaxKind.TildeToken, false)]
    public void TestParsingUnaryExpression(string input, SyntaxKind expectedOperatorKind, bool trailing)
    {
        var unaryExpression = TestUtils.ParseExpression<UnaryExpression>(input);

        unaryExpression.Should().NotBeNull();
        unaryExpression.Operator.Kind.Should().Be(expectedOperatorKind);
        unaryExpression.Trailing.Should().Be(trailing);

        unaryExpression.Operand.Should().BeOfType<NameExpression>();
    }

    [Theory]
    [InlineData("+TestClass.PublicStaticIntProperty", SyntaxKind.PlusToken, false)]
    [InlineData("-TestClass.PublicStaticIntProperty", SyntaxKind.MinusToken, false)]
    [InlineData("++TestClass.PublicStaticIntProperty", SyntaxKind.PlusPlusToken, false)]
    [InlineData("--TestClass.PublicStaticIntProperty", SyntaxKind.MinusMinusToken, false)]
    [InlineData("TestClass.PublicStaticIntProperty++", SyntaxKind.PlusPlusToken, true)]
    [InlineData("TestClass.PublicStaticIntProperty--", SyntaxKind.MinusMinusToken, true)]
    [InlineData("!TestClass.PublicStaticBoolProperty", SyntaxKind.BangToken, false)]
    [InlineData("~TestClass.PublicStaticIntProperty", SyntaxKind.TildeToken, false)]
    public void TestParsingUnaryExpressionWithMemberAccessExpression(string input, SyntaxKind expectedOperatorKind, bool trailing)
    {
        var unaryExpression = TestUtils.ParseExpression<UnaryExpression>(input);

        unaryExpression.Should().NotBeNull();
        unaryExpression.Operator.Kind.Should().Be(expectedOperatorKind);
        unaryExpression.Trailing.Should().Be(trailing);

        unaryExpression.Operand.Should().BeOfType<MemberAccessExpression>();
    }
}
