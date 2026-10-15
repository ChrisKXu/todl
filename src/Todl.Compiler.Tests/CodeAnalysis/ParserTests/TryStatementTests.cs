using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class TryStatementTests
{
    [Fact]
    public void TryStatementCanHaveTypedExceptClauseWithVariable()
    {
        var tryStatement = TestUtils.ParseStatement<TryStatement>("try { a(); } except FormatException e { b(); }");

        tryStatement.BlockStatement.InnerStatements.Should().HaveCount(1);
        tryStatement.FinallyClause.Should().BeNull();

        var exceptClause = tryStatement.ExceptClauses.Should().ContainSingle().Subject;
        exceptClause.IsGeneralClause.Should().BeFalse();
        exceptClause.TypeExpression.GetText().Should().Be("FormatException");
        exceptClause.IdentifierToken.Value.Text.ToString().Should().Be("e");
        exceptClause.BlockStatement.InnerStatements.Should().HaveCount(1);
    }

    [Fact]
    public void ExceptClauseVariableIsOptional()
    {
        var exceptClause = TestUtils
            .ParseStatement<TryStatement>("try { } except System::FormatException { }")
            .ExceptClauses.Single();

        exceptClause.TypeExpression.GetText().Should().Be("System::FormatException");
        exceptClause.IdentifierToken.Should().BeNull();
    }

    [Fact]
    public void ExceptKeywordFollowedByBraceIsGeneralClause()
    {
        var exceptClause = TestUtils
            .ParseStatement<TryStatement>("try { } except { a(); }")
            .ExceptClauses.Single();

        exceptClause.IsGeneralClause.Should().BeTrue();
        exceptClause.TypeExpression.Should().BeNull();
        exceptClause.IdentifierToken.Should().BeNull();
        exceptClause.BlockStatement.InnerStatements.Should().HaveCount(1);
    }

    [Fact]
    public void TryStatementCanHaveMultipleExceptClausesAndFinallyClause()
    {
        var tryStatement = TestUtils.ParseStatement<TryStatement>(
            "try { } except A a { } except B { } except { } finally { c(); }");

        tryStatement.ExceptClauses.Should().HaveCount(3);
        tryStatement.ExceptClauses.Select(c => c.IsGeneralClause).Should().Equal(false, false, true);
        tryStatement.FinallyClause.BlockStatement.InnerStatements.Should().HaveCount(1);
        tryStatement.GetText().Should().Be("try { } except A a { } except B { } except { } finally { c(); }");
    }

    [Fact]
    public void TryStatementCanHaveMultipleExceptClausesWithoutFinallyClause()
    {
        var tryStatement = TestUtils.ParseStatement<TryStatement>("try { } except A { } except B b { }");

        tryStatement.ExceptClauses.Should().HaveCount(2);
        tryStatement.FinallyClause.Should().BeNull();
        tryStatement.GetText().Should().Be("try { } except A { } except B b { }");
    }

    [Fact]
    public void TryStatementCanHaveOnlyFinallyClause()
    {
        var tryStatement = TestUtils.ParseStatement<TryStatement>("try { } finally { }");

        tryStatement.ExceptClauses.Should().BeEmpty();
        tryStatement.FinallyClause.Should().NotBeNull();
    }

    [Fact]
    public void TryStatementsCanBeNested()
    {
        var tryStatement = TestUtils.ParseStatement<TryStatement>("try { try { } finally { } } except { try { } except { } }");

        tryStatement.BlockStatement.InnerStatements.Single().Should().BeOfType<TryStatement>();
        tryStatement.ExceptClauses.Single().BlockStatement.InnerStatements.Single().Should().BeOfType<TryStatement>();
    }

    [Theory]
    [InlineData("try { }")]
    [InlineData("try { } a();")]
    public void TryStatementRequiresAtLeastOneHandlerClause(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var tryStatement = TestUtils.ParseStatement<TryStatement>(inputText, diagnosticBuilder);

        tryStatement.ExceptClauses.Should().BeEmpty();
        tryStatement.FinallyClause.Should().BeNull();

        var diagnostic = diagnosticBuilder.Build().Should().ContainSingle().Subject;
        diagnostic.ErrorCode.Should().Be(ErrorCode.UnexpectedToken);
        diagnostic.TextLocation.GetText().Should().Be("try");
    }

    [Fact]
    public void ExceptClauseCannotFollowFinallyClause()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var tryStatement = TestUtils.ParseStatement<TryStatement>("try { } finally { } except A { }", diagnosticBuilder);

        tryStatement.FinallyClause.Should().NotBeNull();
        tryStatement.ExceptClauses.Should().HaveCount(1);

        var diagnostic = diagnosticBuilder.Build().Should().ContainSingle().Subject;
        diagnostic.ErrorCode.Should().Be(ErrorCode.UnexpectedToken);
        diagnostic.TextLocation.GetText().Should().Be("except");
    }

    [Fact]
    public void TryStatementCannotHaveMoreThanOneFinallyClause()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.ParseStatement<TryStatement>("try { } finally { } finally { }", diagnosticBuilder);

        var diagnostic = diagnosticBuilder.Build().Should().ContainSingle().Subject;
        diagnostic.ErrorCode.Should().Be(ErrorCode.UnexpectedToken);
        diagnostic.TextLocation.GetText().Should().Be("finally");
    }

    [Fact]
    public void ExceptClauseHeadCannotBeParenthesized()
    {
        var tryStatement = TestUtils.ParseStatement<TryStatement>("try { } except (FormatException e) { }", new DiagnosticBag.Builder());

        // the parenthesis cannot start a type name, so the type's identifier is missing
        tryStatement.ExceptClauses.Single().TypeExpression.BaseTypeExpression.As<SimpleNameExpression>()
            .IdentifierToken.Missing.Should().BeTrue();
    }

    [Fact]
    public void TryStatementBlocksAreRequired()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var tryStatement = TestUtils.ParseStatement<TryStatement>("try a(); finally { }", diagnosticBuilder);

        tryStatement.BlockStatement.OpenBraceToken.Missing.Should().BeTrue();
    }
}
