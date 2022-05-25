using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed partial class ParserTests
{
    [Theory]
    [InlineData("if n == 0 { }", SyntaxKind.IfKeywordToken)]
    [InlineData("unless n == 0 { }", SyntaxKind.UnlessKeywordToken)]
    public void IfUnlessStatementsCanHaveSimpleConditionAndNoInnerStatements(string inputText, SyntaxKind expectedSyntaxKind)
    {
        var ifUnlessStatement = ParseStatement<IfUnlessStatement>(inputText);

        ifUnlessStatement.Should().NotBeNull();
        ifUnlessStatement.IfOrUnlessToken.Kind.Should().Be(expectedSyntaxKind);
        ifUnlessStatement.ElseClauses.Should().BeEmpty();
        ifUnlessStatement.BlockStatement.InnerStatements.Should().BeEmpty();

        var condition = ifUnlessStatement.ConditionExpression.As<BinaryExpression>();
        condition.Should().NotBeNull();

        condition.Left.As<NameExpression>().Text.ToString().Should().Be("n");
        condition.Operator.Kind.Should().Be(SyntaxKind.EqualsEqualsToken);
        condition.Right.As<LiteralExpression>().Text.ToString().Should().Be("0");
    }

    [Theory]
    [InlineData("if n == 0 { return n; }", 1)]
    [InlineData("if n == 0 { n = n + 1; return n; }", 2)]
    public void IfUnlessStatementsCanHaveOneOrMoreInnerStatements(string inputText, int expectedInnerStatements)
    {
        var ifUnlessStatement = ParseStatement<IfUnlessStatement>(inputText);

        ifUnlessStatement.Should().NotBeNull();
        ifUnlessStatement.IfOrUnlessToken.Kind.Should().Be(SyntaxKind.IfKeywordToken);
        ifUnlessStatement.ElseClauses.Should().BeEmpty();
        ifUnlessStatement.BlockStatement.InnerStatements.Should().HaveCount(expectedInnerStatements);
    }

    [Theory]
    [InlineData("if (n == 0) { }", SyntaxKind.IfKeywordToken)]
    [InlineData("unless (n == 0) { }", SyntaxKind.UnlessKeywordToken)]
    public void IfUnlessStatementsCanHaveParenthesisAroundConditions(string inputText, SyntaxKind expectedSyntaxKind)
    {
        var ifUnlessStatement = ParseStatement<IfUnlessStatement>(inputText);

        ifUnlessStatement.Should().NotBeNull();
        ifUnlessStatement.IfOrUnlessToken.Kind.Should().Be(expectedSyntaxKind);
        ifUnlessStatement.ElseClauses.Should().BeEmpty();

        var condition = ifUnlessStatement.ConditionExpression.As<ParethesizedExpression>().InnerExpression.As<BinaryExpression>();
        condition.Should().NotBeNull();

        condition.Left.As<NameExpression>().Text.ToString().Should().Be("n");
        condition.Operator.Kind.Should().Be(SyntaxKind.EqualsEqualsToken);
        condition.Right.As<LiteralExpression>().Text.ToString().Should().Be("0");
    }

    [Theory]
    [InlineData("if n == 0 { return n; } else { return 0; }")]
    [InlineData("unless n == 0 { return n; } else { return 0; }")]
    public void IfUnlessStatementsCanHaveSimpleElseClauses(string inputText)
    {
        var ifUnlessStatement = ParseStatement<IfUnlessStatement>(inputText);
        ifUnlessStatement.Should().NotBeNull();

        ifUnlessStatement.ElseClauses.Should().HaveCount(1);
        ifUnlessStatement.ElseClauses[0].BlockStatement.InnerStatements.Should().HaveCount(1);
        var returnStatement = ifUnlessStatement.ElseClauses[0].BlockStatement.InnerStatements[0].As<ReturnStatement>();
        returnStatement.ReturnValueExpression.Text.Should().Be("0");
    }

    [Theory]
    [InlineData("if n == 0 { return n; } else if n == 1 { return n + 1; } else { return 0; }", 2)]
    [InlineData("unless a == b { return 0; } else unless a + b == 0 { return 1; } else unless a == 0 { return 2; } else { return 3; }", 3)]
    public void IfUnlessStatementsCanHaveMultipleElseClauses(string inputText, int expectedCount)
    {
        var ifUnlessStatement = ParseStatement<IfUnlessStatement>(inputText);
        ifUnlessStatement.Should().NotBeNull();
        ifUnlessStatement.ElseClauses.Should().HaveCount(expectedCount);
        ifUnlessStatement.GetDiagnostics().Should().BeEmpty();
    }

    [Fact]
    public void IfUnlessStatementsCanHaveComplexConditions()
    {
        var inputText = "if a == 0 && (b.IsUpper() || string.IsNullOrEmpty(c)) { }";
        var ifUnlessStatement = ParseStatement<IfUnlessStatement>(inputText);
        ifUnlessStatement.Should().NotBeNull();
        ifUnlessStatement.IfOrUnlessToken.Kind.Should().Be(SyntaxKind.IfKeywordToken);

        var condition = ifUnlessStatement.ConditionExpression.As<BinaryExpression>();
        condition.Should().NotBeNull();

        condition.Left.As<BinaryExpression>().Invoking(left =>
        {
            left.Left.As<NameExpression>().Text.Should().Be("a");
            left.Operator.Kind.Should().Be(SyntaxKind.EqualsEqualsToken);
            left.Right.As<LiteralExpression>().Text.Should().Be("0");
        });

        condition.Operator.Kind.Should().Be(SyntaxKind.AmpersandAmpersandToken);

        condition
            .Right
            .As<ParethesizedExpression>()
            .InnerExpression
            .As<BinaryExpression>()
            .Invoking(right =>
        {
            right.Left.As<FunctionCallExpression>().Should().NotBeNull();
            right.Operator.Kind.Should().Be(SyntaxKind.PipePipeToken);
            right.Right.As<FunctionCallExpression>().Should().NotBeNull();
        });
    }
}
