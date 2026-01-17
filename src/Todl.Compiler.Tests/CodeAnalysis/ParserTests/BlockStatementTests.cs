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
                a = a + 1;
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
                a = a + 1;
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

    [Fact]
    public void TestEmptyBlockStatement()
    {
        var blockStatement = TestUtils.ParseStatement<BlockStatement>("{ }");

        blockStatement.Should().NotBeNull();
        blockStatement.OpenBraceToken.Kind.Should().Be(SyntaxKind.OpenBraceToken);
        blockStatement.CloseBraceToken.Kind.Should().Be(SyntaxKind.CloseBraceToken);
        blockStatement.InnerStatements.Should().BeEmpty();
    }

    [Fact]
    public void TestDeeplyNestedBlocks()
    {
        var inputText = "{ { { a = 1; } } }";
        var blockStatement = TestUtils.ParseStatement<BlockStatement>(inputText);

        blockStatement.Should().NotBeNull();
        blockStatement.InnerStatements.Should().HaveCount(1);

        var level1 = blockStatement.InnerStatements[0].As<BlockStatement>();
        level1.Should().NotBeNull();
        level1.InnerStatements.Should().HaveCount(1);

        var level2 = level1.InnerStatements[0].As<BlockStatement>();
        level2.Should().NotBeNull();
        level2.InnerStatements.Should().HaveCount(1);

        level2.InnerStatements[0].Should().BeOfType<ExpressionStatement>();
    }

    [Fact]
    public void TestBlockWithMixedStatements()
    {
        var inputText = @"
            {
                const x = 5;
                let y = x + 1;
                if y > 5 { return y; }
                y = y * 2;
            }
            ";
        var blockStatement = TestUtils.ParseStatement<BlockStatement>(inputText);

        blockStatement.Should().NotBeNull();
        blockStatement.InnerStatements.Should().HaveCount(4);
        blockStatement.InnerStatements[0].Should().BeOfType<VariableDeclarationStatement>();
        blockStatement.InnerStatements[1].Should().BeOfType<VariableDeclarationStatement>();
        blockStatement.InnerStatements[2].Should().BeOfType<IfUnlessStatement>();
        blockStatement.InnerStatements[3].Should().BeOfType<ExpressionStatement>();
    }

    [Fact]
    public void TestBlockWithSingleStatement()
    {
        var blockStatement = TestUtils.ParseStatement<BlockStatement>("{ return; }");

        blockStatement.Should().NotBeNull();
        blockStatement.InnerStatements.Should().HaveCount(1);
        blockStatement.InnerStatements[0].Should().BeOfType<ReturnStatement>();
    }

    [Fact]
    public void TestAdjacentBlocks()
    {
        var inputText = @"
            {
                { a = 1; }
                { b = 2; }
            }
            ";
        var blockStatement = TestUtils.ParseStatement<BlockStatement>(inputText);

        blockStatement.Should().NotBeNull();
        blockStatement.InnerStatements.Should().HaveCount(2);
        blockStatement.InnerStatements[0].Should().BeOfType<BlockStatement>();
        blockStatement.InnerStatements[1].Should().BeOfType<BlockStatement>();
    }
}
