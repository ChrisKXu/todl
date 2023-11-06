using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
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
    public void BoundLoopStatementCanHaveBody(string inputText, bool negated, int expectedBodyStatementsCount)
    {
        var boundLoopStatement = TestUtils.BindStatement<BoundLoopStatement>(inputText);
        boundLoopStatement.GetDiagnostics().Should().BeEmpty();
        boundLoopStatement.ConditionNegated.Should().Be(negated);
        boundLoopStatement.Body.As<BoundBlockStatement>().Statements.Count.Should().Be(expectedBodyStatementsCount);
    }

    [Fact]
    public void BoundLoopStatementShouldHaveBooleanConditions()
    {
        var boundLoopStatement = TestUtils.BindStatement<BoundLoopStatement>("while 1 { }");
        boundLoopStatement.GetDiagnostics().Count().Should().Be(1);

        var diagnostic = boundLoopStatement.GetDiagnostics().First();
        diagnostic.Level.Should().Be(DiagnosticLevel.Error);
        diagnostic.ErrorCode.Should().Be(ErrorCode.TypeMismatch);
        diagnostic.Message.Should().Be("Condition must be of boolean type.");
    }
}
