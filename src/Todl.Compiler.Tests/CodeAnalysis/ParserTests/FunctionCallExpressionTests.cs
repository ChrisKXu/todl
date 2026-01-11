using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class FunctionCallExpressionTests
{
    private void PerformBasicValidationForFunctionCallExpression(FunctionCallExpression functionCallExpression)
    {
        functionCallExpression.DotToken.Text.Should().Be(".");
        functionCallExpression.DotToken.Kind.Should().Be(SyntaxKind.DotToken);
        functionCallExpression.NameToken.Kind.Should().Be(SyntaxKind.IdentifierToken);

        functionCallExpression.Arguments.OpenParenthesisToken.Text.Should().Be("(");
        functionCallExpression.Arguments.OpenParenthesisToken.Kind.Should().Be(SyntaxKind.OpenParenthesisToken);
        functionCallExpression.Arguments.CloseParenthesisToken.Text.Should().Be(")");
        functionCallExpression.Arguments.CloseParenthesisToken.Kind.Should().Be(SyntaxKind.CloseParenthesisToken);
    }

    [Fact]
    public void TestParseFunctionCallExpressionWithoutArguments()
    {
        var inputText = "a.ToString()";
        var functionCallExpression = TestUtils.ParseExpression<FunctionCallExpression>(inputText);

        PerformBasicValidationForFunctionCallExpression(functionCallExpression);

        functionCallExpression.BaseExpression.As<SimpleNameExpression>().Text.Should().Be("a");
        functionCallExpression.NameToken.Text.Should().Be("ToString");
        functionCallExpression.Arguments.Items.Should().BeEmpty();
    }

    [Fact]
    public void TestParseFunctionCallExpressionWithOnePositionalArgument()
    {
        var inputText = "System::Int32.Parse(\"123\")";
        var functionCallExpression = TestUtils.ParseExpression<FunctionCallExpression>(inputText);

        PerformBasicValidationForFunctionCallExpression(functionCallExpression);

        functionCallExpression.BaseExpression.As<NamespaceQualifiedNameExpression>().Text.Should().Be("System::Int32");
        functionCallExpression.NameToken.Text.Should().Be("Parse");

        functionCallExpression.Arguments.Items.Should().NotBeEmpty();
        functionCallExpression.Arguments.Items.Should().SatisfyRespectively(argument =>
        {
            argument.IsNamedArgument.Should().Be(false);
            argument.Expression.As<LiteralExpression>().Text.Should().Be("\"123\"");
        });
    }

    [Fact]
    public void TestParseFunctionCallExpressionWithOneNamedArgument()
    {
        var inputText = "System::Int32.Parse(s: \"123\")";
        var functionCallExpression = TestUtils.ParseExpression<FunctionCallExpression>(inputText);

        PerformBasicValidationForFunctionCallExpression(functionCallExpression);

        functionCallExpression.BaseExpression.As<NamespaceQualifiedNameExpression>().Text.Should().Be("System::Int32");
        functionCallExpression.NameToken.Text.Should().Be("Parse");

        functionCallExpression.Arguments.Items.Should().NotBeEmpty();
        functionCallExpression.Arguments.Items.Should().SatisfyRespectively(argument =>
        {
            argument.IsNamedArgument.Should().Be(true);
            argument.Identifier?.Text.Should().Be("s");
            argument.Identifier?.Kind.Should().Be(SyntaxKind.IdentifierToken);
            argument.ColonToken?.Text.Should().Be(":");
            argument.ColonToken?.Kind.Should().Be(SyntaxKind.ColonToken);
            argument.Expression.As<LiteralExpression>().Text.Should().Be("\"123\"");
        });
    }

    [Fact]
    public void TestParseFunctionCallExpressionWithoutClosingBracket()
    {
        var inputText = "System::Int32.Parse(s: \"123\"";
        var functionCallExpression = TestUtils.ParseExpression<FunctionCallExpression>(inputText);

        functionCallExpression.Arguments.CloseParenthesisToken.Missing.Should().BeTrue();
        //functionCallExpression.GetDiagnostics().Should().NotBeEmpty();
    }
}
