using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class IncrementDecrementTests
{
    [Theory]
    [InlineData("a[0]++;", "++", "+=")]
    [InlineData("i++;", "++", "+=")]
    [InlineData("x--;", "--", "-=")]
    [InlineData("++i;", "++", "+=")]
    [InlineData("--i;", "--", "-=")]
    public void TestIncrementDecrementReportsDedicatedDiagnostic(string statement, string operatorText, string suggestion)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var expressionStatement = TestUtils.ParseStatement<ExpressionStatement>(statement, diagnosticBuilder);

        var diagnostic = diagnosticBuilder.Build().Should().ContainSingle().Subject;
        diagnostic.ErrorCode.Should().Be(ErrorCode.UnsupportedIncrementDecrementOperator);
        diagnostic.Level.Should().Be(DiagnosticLevel.Error);
        diagnostic.Message.Should().Contain($"'{operatorText}'").And.Contain($"'{suggestion} 1'");
        diagnostic.TextLocation.GetText().Should().Be(operatorText);

        // the operator is dropped and the semicolon is still consumed, so no follow-up parse error.
        expressionStatement.SemicolonToken.Missing.Should().BeFalse();
        expressionStatement.Expression.Should().NotBeOfType<BinaryExpression>().And.NotBeOfType<UnaryExpression>();
    }

    [Fact]
    public void TestPostfixIncrementInsideBinaryExpressionKeepsOperands()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var binaryExpression = TestUtils.ParseExpression<BinaryExpression>("a + b++", diagnosticBuilder);

        diagnosticBuilder.Build().Should().ContainSingle()
            .Which.ErrorCode.Should().Be(ErrorCode.UnsupportedIncrementDecrementOperator);
        binaryExpression.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
        binaryExpression.Right.Should().BeOfType<SimpleNameExpression>();
    }

    [Theory]
    [InlineData("a + +b", SyntaxKind.PlusToken)]
    [InlineData("a - -b", SyntaxKind.MinusToken)]
    public void TestSpaceSeparatedSignsAreStillBinaryAndUnary(string input, SyntaxKind expectedOperatorKind)
    {
        var binaryExpression = TestUtils.ParseExpression<BinaryExpression>(input);

        binaryExpression.Operator.Kind.Should().Be(expectedOperatorKind);
        binaryExpression.Right.As<UnaryExpression>().Operator.Kind.Should().Be(expectedOperatorKind);
    }
}
