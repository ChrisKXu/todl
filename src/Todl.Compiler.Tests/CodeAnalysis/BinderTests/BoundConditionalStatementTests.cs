using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundConditionalStatementTests
{
    [Theory]
    [InlineData("if true { 0.ToString(); }", false)]
    [InlineData("unless true { 0.ToString(); }", true)]
    public void TestBindIfUnlessStatementWithoutElseClauses(string inputText, bool inverted)
    {
        var boundConditionalStatement = TestUtils.BindStatement<BoundConditionalStatement>(inputText);
        boundConditionalStatement.Should().NotBeNull();

        boundConditionalStatement.Condition.As<BoundConstant>().Value.Should().Be(true);

        var blockStatement = (inverted ? boundConditionalStatement.Alternative : boundConditionalStatement.Consequence).As<BoundBlockStatement>();
        blockStatement.Statements.Should().HaveCount(1);
    }

    [Fact]
    public void TestBindIfUnlessStatementWithSimpleElseClause()
    {
        var inputText = "if true { 0.ToString(); } else { 1.ToString(); }";
        var boundConditionalStatement = TestUtils.BindStatement<BoundConditionalStatement>(inputText);
        boundConditionalStatement.Should().NotBeNull();

        boundConditionalStatement.Condition.As<BoundConstant>().Value.Should().Be(true);

        var consequence = boundConditionalStatement.Consequence.As<BoundBlockStatement>();
        consequence.Statements.Should().HaveCount(1);

        var alternative = boundConditionalStatement.Alternative.As<BoundBlockStatement>();
        alternative.Statements.Should().HaveCount(1);
    }

    [Fact]
    public void TestBindIfUnlessStatementWithMultipleElseClauses()
    {
        var inputText = "unless 0 == 1 { 0.ToString(); } else unless 1 == 2 { 1.ToString(); } else { 2.ToString(); }";
        var boundConditionalStatement = TestUtils.BindStatement<BoundConditionalStatement>(inputText);
        boundConditionalStatement.Should().NotBeNull();

        void ValidateCondition(BoundConditionalStatement boundConditionalStatement, int expectedLeft, int expectedRight)
        {
            boundConditionalStatement.Condition.As<BoundBinaryExpression>().Invoking(b =>
            {
                b.Left.As<BoundConstant>().Value.Should().Be(expectedLeft);
                b.Right.As<BoundConstant>().Value.Should().Be(expectedRight);
                b.Operator.BoundBinaryOperatorKind.Should().Be(BoundBinaryOperatorKind.Equality);
            });
        }

        void ValidateAlternative(BoundConditionalStatement boundConditionalStatement, int expectedValue)
        {
            var boundBlockStatement = boundConditionalStatement.Alternative.As<BoundBlockStatement>();
            ValidateBlockStatements(boundBlockStatement, expectedValue);
        }

        void ValidateBlockStatements(BoundBlockStatement boundBlockStatement, int expectedValue)
        {
            var expressionStatement = boundBlockStatement.Statements[0].As<BoundExpressionStatement>();
            expressionStatement.Expression.As<BoundClrFunctionCallExpression>().Invoking(func =>
            {
                func.BoundBaseExpression.As<BoundConstant>().Value.Should().Be(expectedValue);
                func.BoundArguments.Should().BeEmpty();
                func.MethodInfo.Name.Should().Be("ToString");
            });
        }

        // 0 == 1
        ValidateCondition(boundConditionalStatement, 0, 1);

        // 0.ToString()
        ValidateAlternative(boundConditionalStatement, 0);

        // ... else unless 1 == 2 ...
        var consequence = boundConditionalStatement.Consequence.As<BoundConditionalStatement>();
        ValidateCondition(consequence, 1, 2);

        // 1.ToString()
        ValidateAlternative(consequence, 1);

        // 2.ToString()
        ValidateBlockStatements(consequence.Consequence.As<BoundBlockStatement>(), 2);
    }

    [Theory]
    [InlineData("if 0 { 0.ToString(); }")]
    public void BoundConditionalStatementShouldHaveBooleanConditions(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundConditionalStatement = TestUtils.BindStatement<BoundConditionalStatement>(inputText, diagnosticBuilder);
        boundConditionalStatement.Should().NotBeNull();

        diagnosticBuilder.Build().First().ErrorCode.Should().Be(ErrorCode.TypeMismatch);
    }

    [Fact]
    public void BoundConditionalStatementShouldHaveCorrectScope()
    {
        var inputText = @"
            void func() {
                const a = 0;
                let b = a + 4;
                if true {
                    let a = 20;
                    a += 20;
                    b = 20;
                }
                b = a + 5;
            }";
        var func = TestUtils.BindMember<BoundFunctionMember>(inputText);

        var conditionalStatement = func.Body.Statements[2].As<BoundConditionalStatement>();
        var consequence = conditionalStatement.Consequence.As<BoundBlockStatement>();
        consequence.Scope.Parent.Should().Be(func.FunctionScope);

        var outerA = func.FunctionScope.LookupVariable("a");
        var outerB = func.FunctionScope.LookupVariable("b");
        var innerA = consequence.Scope.LookupVariable("a");
        var innerB = consequence.Scope.LookupVariable("b");

        outerA.Should().NotBe(innerA);
        outerB.Should().Be(innerB);
    }
}
