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
        boundLoopStatement.ConditionNegated.Should().Be(negated);
        boundLoopStatement.Body.As<BoundBlockStatement>().Statements.Should().HaveCount(expectedBodyStatementsCount);
        boundLoopStatement.BoundLoopContext.Should().NotBeNull();
    }

    [Fact]
    public void BoundLoopStatementsShouldHaveBooleanConditions()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundLoopStatement = TestUtils.BindStatement<BoundLoopStatement>("while 1 { }", diagnosticBuilder);
        boundLoopStatement.Should().NotBeNull();
        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Count().Should().Be(1);

        var diagnostic = diagnostics.First();
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
        TestUtils.BindStatement<BoundLoopStatement>(inputText).Should().NotBeNull();
    }

    [Fact]
    public void BoundLoopStatementsCanHaveNamedLoopContext()
    {
        var boundLoopStatement = TestUtils.BindStatement<BoundLoopStatement>("while true: loop { }");
        boundLoopStatement.Should().NotBeNull();
        boundLoopStatement.BoundLoopContext.Should().NotBeNull();
        boundLoopStatement.BoundLoopContext.LoopLabel.Label.Text.Should().Be("loop");
    }

    [Fact]
    public void LoopContextCanBeStacked()
    {
        // This test makes sure that
        // 1. loop labels can be stacked
        // 2. loop labels can be reused in different scopes
        // 3. we can have mixed loop labels and unnamed loops
        var loops = TestUtils.BindStatement<BoundBlockStatement>(
            @"{
                while true: loop1 { 
                    while true: loop2 { 
                        while true: loop3 { } 
                    } 
                }

                while true: loop2 { 
                    while true: loop3 { }
                }

                while true {
                    while true: inner { }
                }
            }");

        loops.Should().NotBeNull();

        var loop1_1 = loops.Statements[0].As<BoundLoopStatement>();
        loop1_1.BoundLoopContext.LoopLabel.Label.Text.Should().Be("loop1");
        loop1_1.BoundLoopContext.Parent.Should().BeNull();
        var loop1_2 = loop1_1.Body.As<BoundBlockStatement>().Statements[0].As<BoundLoopStatement>();
        loop1_2.BoundLoopContext.LoopLabel.Label.Text.Should().Be("loop2");
        loop1_2.BoundLoopContext.Parent.Should().Be(loop1_1.BoundLoopContext);
        var loop1_3 = loop1_2.Body.As<BoundBlockStatement>().Statements[0].As<BoundLoopStatement>();
        loop1_3.BoundLoopContext.LoopLabel.Label.Text.Should().Be("loop3");
        loop1_3.BoundLoopContext.Parent.Should().Be(loop1_2.BoundLoopContext);

        var loop2_1 = loops.Statements[1].As<BoundLoopStatement>();
        loop2_1.BoundLoopContext.LoopLabel.Label.Text.Should().Be("loop2");
        loop2_1.BoundLoopContext.Parent.Should().BeNull();
        var loop2_2 = loop2_1.Body.As<BoundBlockStatement>().Statements[0].As<BoundLoopStatement>();
        loop2_2.BoundLoopContext.LoopLabel.Label.Text.Should().Be("loop3");
        loop2_2.BoundLoopContext.Parent.Should().Be(loop2_1.BoundLoopContext);

        var loop3_1 = loops.Statements[2].As<BoundLoopStatement>();
        loop3_1.BoundLoopContext.LoopLabel.Should().BeNull();
        loop3_1.BoundLoopContext.Parent.Should().BeNull();
        var loop3_2 = loop3_1.Body.As<BoundBlockStatement>().Statements[0].As<BoundLoopStatement>();
        loop3_2.BoundLoopContext.LoopLabel.Label.Text.Should().Be("inner");
        loop3_2.BoundLoopContext.Parent.Should().Be(loop3_1.BoundLoopContext);
    }

    [Fact]
    public void LoopLabelsCannotBeReusedInSameScope()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundLoopStatement = TestUtils.BindStatement<BoundBlockStatement>(
            @"{
                while true: loop { while true: loop { } }
            }", diagnosticBuilder);
        boundLoopStatement.Should().NotBeNull();
        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Count().Should().Be(1);

        var diagnostic = diagnostics.First();
        diagnostic.Level.Should().Be(DiagnosticLevel.Error);
        diagnostic.ErrorCode.Should().Be(ErrorCode.DuplicateLoopLabel);
        diagnostic.Message.Should().Be("Duplicate loop label 'loop'");
    }

    [Theory]
    [InlineData("break;")]
    [InlineData("continue;")]
    public void BreakOrContinueStatementsCanOnlyAppearInLoops(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundStatement = TestUtils.BindStatement<BoundStatement>(inputText, diagnosticBuilder);
        boundStatement.Should().NotBeNull();
        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().NotBeEmpty();

        var noEnclosingLoop = diagnostics.First();
        noEnclosingLoop.Level.Should().Be(DiagnosticLevel.Error);
        noEnclosingLoop.ErrorCode.Should().Be(ErrorCode.NoEnclosingLoop);
        noEnclosingLoop.Message.Should().Be("No enclosing loop out of which to break or continue.");
    }
}
