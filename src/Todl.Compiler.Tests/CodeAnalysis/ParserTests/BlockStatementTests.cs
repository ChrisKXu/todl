using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BlockStatementTests
{
    [Fact]
    public void TestParseBlockStatementBasic()
    {
        var inputText = @"
            {
                a = 1 + 2;
                ++a;
                b = a;
            }
            ";
        var blockStatement = TestUtils.ParseStatement<BlockStatement>(inputText);

        blockStatement.OpenBraceToken.Text.Should().Be("{");
        blockStatement.OpenBraceToken.Kind.Should().Be(SyntaxKind.OpenBraceToken);

        blockStatement.InnerStatements.Should().SatisfyRespectively(
            _0 => _0.As<ExpressionStatement>().Should().NotBeNull(),
            _1 => _1.As<ExpressionStatement>().Should().NotBeNull(),
            _2 => _2.As<ExpressionStatement>().Should().NotBeNull());

        blockStatement.CloseBraceToken.Text.Should().Be("}");
        blockStatement.CloseBraceToken.Kind.Should().Be(SyntaxKind.CloseBraceToken);
    }

    [Fact]
    public void TestParseBlockStatementWithoutClosingBracket()
    {
        var inputText = @"
            {
                a = 1 + 2;
                ++a;
                b = a;
            ";
        var blockStatement = TestUtils.ParseStatement<BlockStatement>(inputText);

        blockStatement.OpenBraceToken.Text.Should().Be("{");
        blockStatement.OpenBraceToken.Kind.Should().Be(SyntaxKind.OpenBraceToken);

        blockStatement.InnerStatements.Should().SatisfyRespectively(
            _0 => _0.As<ExpressionStatement>().Should().NotBeNull(),
            _1 => _1.As<ExpressionStatement>().Should().NotBeNull(),
            _2 => _2.As<ExpressionStatement>().Should().NotBeNull());

        blockStatement.CloseBraceToken.Text.Should().Be("");
        blockStatement.CloseBraceToken.Kind.Should().Be(SyntaxKind.CloseBraceToken);
        blockStatement.CloseBraceToken.Missing.Should().BeTrue();
        //blockStatement.GetDiagnostics().Should().NotBeEmpty();
    }
}
