using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class ThrowStatementTests
{
    [Fact]
    public void ThrowStatementCanHaveExpression()
    {
        var throwStatement = TestUtils.ParseStatement<ThrowStatement>("throw new InvalidOperationException(\"x\");");

        throwStatement.IsRethrow.Should().BeFalse();
        throwStatement.Expression.Should().BeOfType<NewExpression>();
        throwStatement.GetText().Should().Be("throw new InvalidOperationException(\"x\");");
    }

    [Fact]
    public void ThrowStatementWithoutExpressionIsRethrow()
    {
        var throwStatement = TestUtils.ParseStatement<ThrowStatement>("throw;");

        throwStatement.IsRethrow.Should().BeTrue();
        throwStatement.Expression.Should().BeNull();
    }

    [Fact]
    public void ThrowStatementRequiresSemicolon()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var throwStatement = TestUtils.ParseStatement<ThrowStatement>("throw e", diagnosticBuilder);

        throwStatement.SemicolonToken.Missing.Should().BeTrue();
    }

    [Theory]
    [InlineData("try")]
    [InlineData("except")]
    [InlineData("finally")]
    [InlineData("throw")]
    public void ExceptionKeywordsAreReserved(string keyword)
    {
        var statement = TestUtils.ParseStatement<VariableDeclarationStatement>(
            $"const {keyword} = 1;", new DiagnosticBag.Builder());

        statement.IdentifierToken.Missing.Should().BeTrue();
    }
}
