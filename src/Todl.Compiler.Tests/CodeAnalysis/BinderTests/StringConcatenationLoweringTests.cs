using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class StringConcatenationLoweringTests
{
    // Fully constant chains fold away before lowering ever runs.
    [Fact]
    public void FullyConstantChainShouldNotLowerToConcatCall()
    {
        var initializer = LowerLastVariableInitializer("let a = \"p\" + \"q\" + \"r\";");

        initializer.Should().BeOfType<BoundConstant>();
        initializer.As<BoundConstant>().Value.Should().Be("pqr");
    }

    [Fact]
    public void TwoOperandChainShouldLowerToTwoArgConcatCall()
    {
        var initializer = LowerLastVariableInitializer("let a = \"x\"; let b = a + \"y\";");

        var call = initializer.Should().BeOfType<BoundClrInvocationExpression>().Subject;
        call.MethodInfo.GetParameters().Should().HaveCount(2);
        call.BoundArguments.Should().HaveCount(2);
        call.BoundArguments[0].Should().BeOfType<BoundVariableExpression>();
        call.BoundArguments[1].As<BoundConstant>().Value.Should().Be("y");
    }

    [Fact]
    public void ThreeDistinctOperandsShouldLowerToThreeArgConcatCall()
    {
        // "p" and "q" are not adjacent, so they cannot merge.
        var initializer = LowerLastVariableInitializer("let a = \"x\"; let b = \"p\" + a + \"q\";");

        var call = initializer.Should().BeOfType<BoundClrInvocationExpression>().Subject;
        call.MethodInfo.GetParameters().Should().HaveCount(3);
        call.BoundArguments.Should().HaveCount(3);
        call.BoundArguments[0].As<BoundConstant>().Value.Should().Be("p");
        call.BoundArguments[1].Should().BeOfType<BoundVariableExpression>();
        call.BoundArguments[2].As<BoundConstant>().Value.Should().Be("q");
    }

    [Fact]
    public void AdjacentConstantsCreatedByFlatteningShouldMergeIntoOneOperand()
    {
        // "p" and "q" only become adjacent once the chain is flattened.
        var initializer = LowerLastVariableInitializer("let a = \"x\"; let b = a + \"p\" + \"q\";");

        var call = initializer.Should().BeOfType<BoundClrInvocationExpression>().Subject;
        call.MethodInfo.GetParameters().Should().HaveCount(2);
        call.BoundArguments.Should().HaveCount(2);
        call.BoundArguments[0].Should().BeOfType<BoundVariableExpression>();
        call.BoundArguments[1].As<BoundConstant>().Value.Should().Be("pq");
    }

    [Fact]
    public void MoreThanFourOperandsShouldFoldIntoNestedConcatCalls()
    {
        var initializer = LowerLastVariableInitializer(
            "let a = \"1\"; let b = \"2\"; let c = \"3\"; let d = \"4\"; let e = \"5\"; let f = a + b + c + d + e;");

        // Falls back to a leading 4-arg call plus one 2-arg call for the remaining operand.
        var outer = initializer.Should().BeOfType<BoundClrInvocationExpression>().Subject;
        outer.MethodInfo.GetParameters().Should().HaveCount(2);
        outer.BoundArguments.Should().HaveCount(2);

        var inner = outer.BoundArguments[0].Should().BeOfType<BoundClrInvocationExpression>().Subject;
        inner.MethodInfo.GetParameters().Should().HaveCount(4);
        inner.BoundArguments.Should().HaveCount(4);
        inner.BoundArguments.Should().AllBeOfType<BoundVariableExpression>();

        outer.BoundArguments[1].Should().BeOfType<BoundVariableExpression>();
    }

    private static BoundExpression LowerLastVariableInitializer(string statements)
    {
        var blockStatement = TestUtils
            .BindStatement<BoundBlockStatement>("{ " + statements + " }")
            .Accept(new ConstantFoldingBoundTreeRewriter(TestDefaults.ConstantValueFactory))
            .Accept(new StringConcatenationLoweringBoundTreeRewriter(TestDefaults.ConstantValueFactory))
            .As<BoundBlockStatement>();

        return blockStatement.Statements.Last()
            .As<BoundVariableDeclarationStatement>()
            .InitializerExpression;
    }
}
