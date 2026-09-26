using System;
using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundUnaryExpressionTests
{
    [Theory]
    // UnaryPlus
    [InlineData("{ let a = 1; +a; }", BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.Int, SpecialType.ClrInt32)]
    [InlineData("{ let a = 1U; +a; }", BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.UInt, SpecialType.ClrUInt32)]
    [InlineData("{ let a = 1L; +a; }", BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.Long, SpecialType.ClrInt64)]
    [InlineData("{ let a = 1UL; +a; }", BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.ULong, SpecialType.ClrUInt64)]
    [InlineData("{ let a = 1.0F; +a; }", BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.Float, SpecialType.ClrFloat)]
    [InlineData("{ let a = 1.0; +a; }", BoundUnaryOperatorKind.UnaryPlus | BoundUnaryOperatorKind.Double, SpecialType.ClrDouble)]
    // UnaryMinus
    [InlineData("{ let a = 1; -a; }", BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Int, SpecialType.ClrInt32)]
    [InlineData("{ let a = 1U; -a; }", BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.UInt, SpecialType.ClrInt64)]
    [InlineData("{ let a = 1L; -a; }", BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Long, SpecialType.ClrInt64)]
    [InlineData("{ let a = 1.0F; -a; }", BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Float, SpecialType.ClrFloat)]
    [InlineData("{ let a = 1.0; -a; }", BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Double, SpecialType.ClrDouble)]
    // LogicalNegation
    [InlineData("{ let a = true; !a; }", BoundUnaryOperatorKind.LogicalNegation | BoundUnaryOperatorKind.Bool, SpecialType.ClrBoolean)]
    // BitwiseComplement
    [InlineData("{ let a = 1; ~a; }", BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.Int, SpecialType.ClrInt32)]
    [InlineData("{ let a = 1U; ~a; }", BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.UInt, SpecialType.ClrUInt32)]
    [InlineData("{ let a = 1L; ~a; }", BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.Long, SpecialType.ClrInt64)]
    [InlineData("{ let a = 1UL; ~a; }", BoundUnaryOperatorKind.BitwiseComplement | BoundUnaryOperatorKind.ULong, SpecialType.ClrUInt64)]
    public void TestBindUnaryExpressionWithSupportedSpecialTypes(string input, BoundUnaryOperatorKind expectedOperatorKind, SpecialType expectedSpecialType)
    {
        var boundBlockStatement = TestUtils.BindStatement<BoundBlockStatement>(input);
        var boundExpressionStatement = boundBlockStatement.Statements[^1].As<BoundExpressionStatement>();
        var boundUnaryExpression = boundExpressionStatement.Expression.As<BoundUnaryExpression>();

        boundUnaryExpression.Should().NotBeNull();
        boundUnaryExpression.Operator.BoundUnaryOperatorKind.Should().Be(expectedOperatorKind);
        boundUnaryExpression.ResultType.SpecialType.Should().Be(expectedSpecialType);
        boundUnaryExpression.Operand.As<BoundVariableExpression>().Should().NotBeNull();
    }

    [Theory]
    [InlineData("{ let a = 1UL; -a; }", "-", typeof(ulong))]
    [InlineData("{ let a = 1; !a; }", "!", typeof(int))]
    [InlineData("{ let a = 1.0F; !a; }", "!", typeof(float))]
    [InlineData("{ let a = 1.0; !a; }", "!", typeof(double))]
    [InlineData("{ let a = 1.0F; ~a; }", "~", typeof(float))]
    [InlineData("{ let a = 1.0; ~a; }", "~", typeof(double))]
    [InlineData("{ let a = true; +a; }", "+", typeof(bool))]
    [InlineData("{ let a = false; -a; }", "-", typeof(bool))]
    [InlineData("{ let a = \"abc\"; -a; }", "-", typeof(string))]
    public void TestBindUnaryExpressionWithMismatchedSpecialTypes(string input, string operatorText, Type operandType)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundBlockStatement = TestUtils.BindStatement<BoundBlockStatement>(input, diagnosticBuilder);
        var boundExpressionStatement = boundBlockStatement.Statements[^1].As<BoundExpressionStatement>();
        var boundUnaryExpression = boundExpressionStatement.Expression.As<BoundUnaryExpression>();

        boundUnaryExpression.Should().NotBeNull();

        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().NotBeEmpty();

        var diagnostic = diagnostics.First();
        diagnostic.Message.Should().Be($"Unary operator \"{operatorText}\" is not supported on type \"{operandType.FullName}\"");
        diagnostic.ErrorCode.Should().Be(ErrorCode.UnsupportedOperator);
    }
}
