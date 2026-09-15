using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.Diagnostics;
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

    [Theory]
    [InlineData("1 + \" apples\"", true)]
    [InlineData("\"count: \" + 1", false)]
    [InlineData("true + \" flag\"", true)]
    [InlineData("\"flag: \" + true", false)]
    [InlineData("3.5 + \" pi\"", true)]
    [InlineData("\"pi: \" + 3.5", false)]
    public void TestBindStringConcatenationWithValueTypeOperand(string input, bool leftIsValueType)
    {
        var boundBinaryExpression = TestUtils.BindExpression<BoundBinaryExpression>(input);

        boundBinaryExpression.Operator.BoundBinaryOperatorKind.Should().Be(BoundBinaryOperatorKind.StringConcatenation);
        boundBinaryExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrString);

        var convertedOperand = leftIsValueType ? boundBinaryExpression.Left : boundBinaryExpression.Right;
        var stringOperand = leftIsValueType ? boundBinaryExpression.Right : boundBinaryExpression.Left;

        var toStringCall = convertedOperand.Should().BeOfType<BoundClrFunctionCallExpression>().Subject;
        toStringCall.MethodInfo.Name.Should().Be("ToString");
        toStringCall.MethodInfo.GetParameters().Should().BeEmpty();
        toStringCall.IsStatic.Should().BeFalse();

        stringOperand.ResultType.SpecialType.Should().Be(SpecialType.ClrString);
    }

    [Fact]
    public void TestBindStringConcatenationWithReferenceTypeOperandIsUnsupported()
    {
        // Scope boundary: reference-typed operands aren't widened to string concatenation -
        // only built-in value types are, since a null reference-typed operand would crash
        // calling ToString() on it with no null-conditional support to guard it.
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundBinaryExpression = TestUtils.BindExpression<BoundBinaryExpression>(
            "\"err: \" + new System::Exception()", diagnosticBuilder);

        boundBinaryExpression.Should().NotBeNull();
        boundBinaryExpression.Operator.Should().BeNull();

        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().ContainSingle();
        diagnostics.Single().ErrorCode.Should().Be(ErrorCode.UnsupportedOperator);
    }
}
