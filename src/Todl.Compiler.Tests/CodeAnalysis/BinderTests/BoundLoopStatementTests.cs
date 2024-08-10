using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundLoopStatementTests
{
    [Theory]
    [InlineData("while true { }", false, 0)]
    [InlineData("until true { }", true, 0)]
    [InlineData("while true { 0.ToString(); }", false, 1)]
    [InlineData("while true { 0.ToString(); const a = 1; }", false, 2)]
    public void BoundLoopStatementsCanHaveBody(string inputText, bool negated, int expectedBodyStatementsCount)
    {
        var boundLoopStatement = TestUtils.BindStatement<BoundLoopStatement>(inputText);
        boundLoopStatement.GetDiagnostics().Should().BeEmpty();
        boundLoopStatement.ConditionNegated.Should().Be(negated);
        boundLoopStatement.Body.As<BoundBlockStatement>().Statements.Should().HaveCount(expectedBodyStatementsCount);
        boundLoopStatement.BoundLoopContext.Should().NotBeNull();
    }

    [Fact]
    public void BoundLoopStatementsShouldHaveBooleanConditions()
    {
        var boundLoopStatement = TestUtils.BindStatement<BoundLoopStatement>("while 1 { }");
        boundLoopStatement.GetDiagnostics().Count().Should().Be(1);

        var diagnostic = boundLoopStatement.GetDiagnostics().First();
        diagnostic.Level.Should().Be(DiagnosticLevel.Error);
        diagnostic.ErrorCode.Should().Be(ErrorCode.TypeMismatch);
        diagnostic.Message.Should().Be("Condition must be of boolean type.");
    }

    [Theory]
    [InlineData("while true { break; }")]
    [InlineData("while true { continue; }")]
    [InlineData("while 0 < 1 { if 1 < 2 { break; } else { continue; } }")]
    public void BoundLoopStatementsCanHaveBreakOrContinueStatements(string inputText)
    {
        var boundLoopStatement = TestUtils.BindStatement<BoundLoopStatement>(inputText);
        boundLoopStatement.GetDiagnostics().Should().BeEmpty();
    }

    [Theory]
    [InlineData("break;")]
    [InlineData("continue;")]
    public void BreakOrContinueStatementsCanOnlyAppearInLoops(string inputText)
    {
        var boundStatement = TestUtils.BindStatement<BoundStatement>(inputText);
        var diagnostics = boundStatement.GetDiagnostics();
        diagnostics.Should().NotBeEmpty();

        var noEnclosingLoop = diagnostics.First();
        noEnclosingLoop.Level.Should().Be(DiagnosticLevel.Error);
        noEnclosingLoop.ErrorCode.Should().Be(ErrorCode.NoEnclosingLoop);
        noEnclosingLoop.Message.Should().Be("No enclosing loop out of which to break or continue.");
    }
}
