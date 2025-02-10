using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class ConstantFoldingTests
{
    [Theory]
    [InlineData("const a = +10;", 10)]
    [InlineData("const a = +10U;", 10U)]
    [InlineData("const a = +10L;", 10L)]
    [InlineData("const a = +10UL;", 10UL)]
    [InlineData("const a = +1.0F;", 1.0F)]
    [InlineData("const a = +1.0;", 1.0)]
    [InlineData("const a = -10;", -10)]
    [InlineData("const a = -10U;", -10U)]
    [InlineData("const a = -10L;", -10L)]
    [InlineData("const a = -1.0F;", -1.0F)]
    [InlineData("const a = -1.0;", -1.0)]
    [InlineData("const a = !true;", false)]
    [InlineData("const a = !false;", true)]
    [InlineData("const a = ~10;", ~10)]
    [InlineData("const a = ~10U;", ~10U)]
    [InlineData("const a = ~10L;", ~10L)]
    [InlineData("const a = ~10UL;", ~10UL)]
    public void ConstantFoldingUnaryOperatorTest(string inputText, object expectedValue)
    {
        var constantFoldingBoundNodeVisitor = new ConstantFoldingBoundTreeRewriter(TestDefaults.ConstantValueFactory);
        var boundVariableDeclarationStatement = TestUtils
            .BindStatement<BoundVariableDeclarationStatement>(inputText)
            .Accept(constantFoldingBoundNodeVisitor)
            .As<BoundVariableDeclarationStatement>();

        boundVariableDeclarationStatement.Variable.Constant.Should().Be(true);
        var value = boundVariableDeclarationStatement
            .InitializerExpression
            .As<BoundConstant>()
            .Value;

        value.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("const a = 10 + 10;", 20)]
    [InlineData("const a = 10; const b = a + 10;", 20)]
    [InlineData("const a = 10; const b = a * 2;", 20)]
    [InlineData("const a = true;", true)]
    [InlineData("const a = true; const b = a && false", false)]
    [InlineData("const a = -20;", -20)]
    public void BasicConstantFoldingTests(string inputText, object expectedValue)
    {
        var constantFoldingBoundNodeVisitor = new ConstantFoldingBoundTreeRewriter(TestDefaults.ConstantValueFactory);
        var blockStatement = TestUtils
            .BindStatement<BoundBlockStatement>("{ " + inputText + " }")
            .Accept(constantFoldingBoundNodeVisitor)
            .As<BoundBlockStatement>();

        var variableDeclarationStatements = blockStatement.Statements.Select(statement => statement.As<BoundVariableDeclarationStatement>());
        variableDeclarationStatements.Count().Should().Be(blockStatement.Statements.Count());
        variableDeclarationStatements.All(s => s.Variable.Constant).Should().BeTrue();
        variableDeclarationStatements
            .Last()
            .InitializerExpression
            .As<BoundConstant>()
            .Value
            .Should()
            .Be(expectedValue);
    }

    [Theory]
    [InlineData("let a = 10 + 10;")]
    [InlineData("const a = 10; let b = a + 10;")]
    [InlineData("const a = 10; let b = a * 2;")]
    [InlineData("const a = 10; let b = a + 10; const c = a + b;")]
    public void BasicConstantFoldingNegativeTests(string inputText)
    {
        var constantFoldingBoundNodeVisitor = new ConstantFoldingBoundTreeRewriter(TestDefaults.ConstantValueFactory);
        var blockStatement = TestUtils
            .BindStatement<BoundBlockStatement>("{ " + inputText + " }")
            .Accept(constantFoldingBoundNodeVisitor)
            .As<BoundBlockStatement>();

        var boundVariableDeclarationStatement = blockStatement.Statements.Last().As<BoundVariableDeclarationStatement>();
        boundVariableDeclarationStatement.Variable.Constant.Should().Be(false);
    }

    [Fact]
    public void PartiallyFoldedConstantTests()
    {
        var inputText = "let a = 10 + 10;";
        var constantFoldingBoundNodeVisitor = new ConstantFoldingBoundTreeRewriter(TestDefaults.ConstantValueFactory);
        var boundVariableDeclarationStatement = TestUtils
            .BindStatement<BoundVariableDeclarationStatement>(inputText)
            .Accept(constantFoldingBoundNodeVisitor)
            .As<BoundVariableDeclarationStatement>();

        boundVariableDeclarationStatement.Variable.Constant.Should().Be(false);
        boundVariableDeclarationStatement.InitializerExpression.Constant.Should().Be(true);
        boundVariableDeclarationStatement.InitializerExpression.As<BoundConstant>().Value.Should().Be(20);
    }
}
