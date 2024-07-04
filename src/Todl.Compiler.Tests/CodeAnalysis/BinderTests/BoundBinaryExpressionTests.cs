using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundBinaryExpressionTests
{
    [Fact]
    public void TestBindBinaryExpression()
    {
        var boundBinaryExpression = TestUtils.BindExpression<BoundBinaryExpression>("1 + 2 + 3");

        boundBinaryExpression.Should().NotBeNull();
        boundBinaryExpression.Operator.BoundBinaryOperatorKind.Should().Be(BoundBinaryOperatorKind.NumericAddition);
        boundBinaryExpression.Right.As<BoundConstant>().Value.Should().Be(3);

        var left = boundBinaryExpression.Left.As<BoundBinaryExpression>();
        left.Should().NotBeNull();
        left.Left.As<BoundConstant>().Value.Should().Be(1);
        left.Right.As<BoundConstant>().Value.Should().Be(2);
        left.Operator.BoundBinaryOperatorKind.Should().Be(BoundBinaryOperatorKind.NumericAddition);
    }

    [Fact]
    public void TestBindBinaryExpression2()
    {
        var boundBinaryExpression = TestUtils.BindExpression<BoundBinaryExpression>("1 + 2 * 3 >= 4");

        boundBinaryExpression.Should().NotBeNull();
        boundBinaryExpression.Operator.BoundBinaryOperatorKind.Should().Be(BoundBinaryOperatorKind.Comparison);
        boundBinaryExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrBoolean);
        boundBinaryExpression.Right.As<BoundConstant>().Value.Should().Be(4);

        var left = boundBinaryExpression.Left.As<BoundBinaryExpression>();
        left.Should().NotBeNull();
        left.Left.As<BoundConstant>().Value.Should().Be(1);
        left.Operator.BoundBinaryOperatorKind.Should().Be(BoundBinaryOperatorKind.NumericAddition);

        var multiplication = left.Right.As<BoundBinaryExpression>();
        multiplication.Left.As<BoundConstant>().Value.Should().Be(2);
        multiplication.Right.As<BoundConstant>().Value.Should().Be(3);
        multiplication.Operator.BoundBinaryOperatorKind.Should().Be(BoundBinaryOperatorKind.NumericMultiplication);
    }
}
