using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed partial class ParserTests
{
    private void PerformBasicValidationForNewExpression(NewExpression newExpression)
    {
        newExpression.NewKeywordToken.Text.Should().Be("new");
        newExpression.NewKeywordToken.Kind.Should().Be(SyntaxKind.NewKeywordToken);

        newExpression.Arguments.OpenParenthesisToken.Text.Should().Be("(");
        newExpression.Arguments.OpenParenthesisToken.Kind.Should().Be(SyntaxKind.OpenParenthesisToken);
        newExpression.Arguments.CloseParenthesisToken.Text.Should().Be(")");
        newExpression.Arguments.CloseParenthesisToken.Kind.Should().Be(SyntaxKind.CloseParenthesisToken);
    }

    [Fact]
    public void TestParseNewExpressionBasicWithNoArguments()
    {
        var inputText = "new System.Exception()";
        var newExpression = ParseExpression<NewExpression>(inputText);

        PerformBasicValidationForNewExpression(newExpression);
        newExpression.TypeNameExpression.Text.Should().Be("System.Exception");
        newExpression.Arguments.Items.Should().BeEmpty();
    }

    [Fact]
    public void TestParseNewExpressionBasicWithOnePositionalArgument()
    {
        var inputText = "new System.Uri(\"https://google.com\")";
        var newExpression = ParseExpression<NewExpression>(inputText);

        PerformBasicValidationForNewExpression(newExpression);
        newExpression.TypeNameExpression.Text.Should().Be("System.Uri");
        newExpression.Arguments.Items.Should().SatisfyRespectively(argument =>
        {
            argument.IsNamedArgument.Should().BeFalse();
            argument.Expression.As<LiteralExpression>().Text.Should().Be("\"https://google.com\"");
        });
    }

    [Fact]
    public void TestParseNewExpressionBasicWithOneNamedArgument()
    {
        var inputText = "new System.Uri(uriString: \"https://google.com\")";
        var newExpression = ParseExpression<NewExpression>(inputText);

        PerformBasicValidationForNewExpression(newExpression);
        newExpression.TypeNameExpression.Text.Should().Be("System.Uri");
        newExpression.Arguments.Items.Should().SatisfyRespectively(argument =>
        {
            argument.IsNamedArgument.Should().BeTrue();
            argument.Identifier.Text.Should().Be("uriString");
            argument.Expression.As<LiteralExpression>().Text.Should().Be("\"https://google.com\"");
        });
    }

    [Fact]
    public void TestParseNewExpressionBasicWithMultiplePositionalArguments()
    {
        var inputText = "new System.Uri(\"https://google.com\", false)";
        var newExpression = ParseExpression<NewExpression>(inputText);

        PerformBasicValidationForNewExpression(newExpression);
        newExpression.TypeNameExpression.Text.Should().Be("System.Uri");

        newExpression.Arguments.Items.Should().SatisfyRespectively(
            _0 =>
            {
                _0.IsNamedArgument.Should().BeFalse();
                _0.Expression.As<LiteralExpression>().Text.Should().Be("\"https://google.com\"");
            },
            _1 =>
            {
                _1.IsNamedArgument.Should().BeFalse();
                _1.Expression.As<LiteralExpression>().Text.Should().Be("false");
            });
    }

    [Fact]
    public void TestParseNewExpressionBasicWithMultipleNamedArguments()
    {
        var inputText = "new System.Uri(uriString: \"https://google.com\", dontEscape: false)";
        var newExpression = ParseExpression<NewExpression>(inputText);

        PerformBasicValidationForNewExpression(newExpression);
        newExpression.TypeNameExpression.Text.Should().Be("System.Uri");

        newExpression.Arguments.Items.Should().SatisfyRespectively(
            uriString =>
            {
                uriString.IsNamedArgument.Should().BeTrue();
                uriString.Identifier.Text.Should().Be("uriString");
                uriString.Expression.As<LiteralExpression>().Text.Should().Be("\"https://google.com\"");
            },
            dontEscape =>
            {
                dontEscape.IsNamedArgument.Should().BeTrue();
                dontEscape.Identifier.Text.Should().Be("dontEscape");
                dontEscape.Expression.As<LiteralExpression>().Text.Should().Be("false");
            });
    }
}
