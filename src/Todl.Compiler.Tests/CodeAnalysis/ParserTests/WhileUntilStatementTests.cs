using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class WhileUntilStatementTests
{
    [Fact]
    public void EmptyBreakStatementCanBeParsed()
    {
        var breakStatement = TestUtils.ParseStatement<BreakStatement>("break;");
        breakStatement.Should().NotBeNull();
        breakStatement.GetDiagnostics().Should().BeEmpty();

        breakStatement.BreakKeywordToken.Kind.Should().Be(SyntaxKind.BreakKeywordToken);
        breakStatement.SemicolonToken.Kind.Should().Be(SyntaxKind.SemicolonToken);
        breakStatement.Text.Length.Should().Be(6);
    }

    [Fact]
    public void EmptyContinueStatementCanBeParsed()
    {
        var continueStatement = TestUtils.ParseStatement<ContinueStatement>("continue;");
        continueStatement.Should().NotBeNull();
        continueStatement.GetDiagnostics().Should().BeEmpty();

        continueStatement.ContinueKeywordToken.Kind.Should().Be(SyntaxKind.ContinueKeywordToken);
        continueStatement.SemicolonToken.Kind.Should().Be(SyntaxKind.SemicolonToken);
        continueStatement.Text.Length.Should().Be(9);
    }

    [Theory]
    [InlineData("while n == 0 { }", SyntaxKind.WhileKeywordToken)]
    [InlineData("until n == 0 { }", SyntaxKind.UntilKeywordToken)]
    public void WhileUntilStatementsCanHaveEmptyBody(string inputText, SyntaxKind expectedSyntaxKind)
    {
        var whileUntilStatement = TestUtils.ParseStatement<WhileUntilStatement>(inputText);
        whileUntilStatement.Should().NotBeNull();
        whileUntilStatement.GetDiagnostics().Should().BeEmpty();

        whileUntilStatement.WhileOrUntilToken.Kind.Should().Be(expectedSyntaxKind);
        whileUntilStatement.BlockStatement.InnerStatements.Should().BeEmpty();

        var condition = whileUntilStatement.ConditionExpression.As<BinaryExpression>();
        condition.Should().NotBeNull();
        condition.Left.As<NameExpression>().Text.ToString().Should().Be("n");
        condition.Operator.Kind.Should().Be(SyntaxKind.EqualsEqualsToken);
        condition.Right.As<LiteralExpression>().Text.ToString().Should().Be("0");
    }

    [Theory]
    [InlineData("while n == 0 { return n; }", 1)]
    [InlineData("until n == 0 { n +=1; return n; }", 2)]
    public void WhileUntilStatementsCanHaveOneOrMoreInnerStatements(string inputText, int expectedStatementsCount)
    {
        var whileUntilStatement = TestUtils.ParseStatement<WhileUntilStatement>(inputText);
        whileUntilStatement.Should().NotBeNull();
        whileUntilStatement.GetDiagnostics().Should().BeEmpty();
        whileUntilStatement.BlockStatement.InnerStatements.Count.Should().Be(expectedStatementsCount);
    }
}
