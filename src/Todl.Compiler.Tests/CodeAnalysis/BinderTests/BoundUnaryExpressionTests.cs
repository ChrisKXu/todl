using System;
using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;
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
    [InlineData("{ let a = 1U; -a; }", BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.UInt, SpecialType.ClrUInt32)]
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
    // PrefixIncrement
    [InlineData("{ let a = 1; ++a; }", BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.Int, SpecialType.ClrInt32)]
    [InlineData("{ let a = 1U; ++a; }", BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.UInt, SpecialType.ClrUInt32)]
    [InlineData("{ let a = 1L; ++a; }", BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.Long, SpecialType.ClrInt64)]
    [InlineData("{ let a = 1UL; ++a; }", BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.ULong, SpecialType.ClrUInt64)]
    [InlineData("{ let a = 1.0F; ++a; }", BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.Float, SpecialType.ClrFloat)]
    [InlineData("{ let a = 1.0; ++a; }", BoundUnaryOperatorKind.PrefixIncrement | BoundUnaryOperatorKind.Double, SpecialType.ClrDouble)]
    // PrefixDecrement
    [InlineData("{ let a = 1; --a; }", BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.Int, SpecialType.ClrInt32)]
    [InlineData("{ let a = 1U; --a; }", BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.UInt, SpecialType.ClrUInt32)]
    [InlineData("{ let a = 1L; --a; }", BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.Long, SpecialType.ClrInt64)]
    [InlineData("{ let a = 1UL; --a; }", BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.ULong, SpecialType.ClrUInt64)]
    [InlineData("{ let a = 1.0F; --a; }", BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.Float, SpecialType.ClrFloat)]
    [InlineData("{ let a = 1.0; --a; }", BoundUnaryOperatorKind.PrefixDecrement | BoundUnaryOperatorKind.Double, SpecialType.ClrDouble)]
    // PostfixIncrement
    [InlineData("{ let a = 1; a++; }", BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.Int, SpecialType.ClrInt32)]
    [InlineData("{ let a = 1U; a++; }", BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.UInt, SpecialType.ClrUInt32)]
    [InlineData("{ let a = 1L; a++; }", BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.Long, SpecialType.ClrInt64)]
    [InlineData("{ let a = 1UL; a++; }", BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.ULong, SpecialType.ClrUInt64)]
    [InlineData("{ let a = 1.0F; a++; }", BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.Float, SpecialType.ClrFloat)]
    [InlineData("{ let a = 1.0; a++; }", BoundUnaryOperatorKind.PostfixIncrement | BoundUnaryOperatorKind.Double, SpecialType.ClrDouble)]
    // PostfixDecrement
    [InlineData("{ let a = 1; a--; }", BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.Int, SpecialType.ClrInt32)]
    [InlineData("{ let a = 1U; a--; }", BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.UInt, SpecialType.ClrUInt32)]
    [InlineData("{ let a = 1L; a--; }", BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.Long, SpecialType.ClrInt64)]
    [InlineData("{ let a = 1UL; a--; }", BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.ULong, SpecialType.ClrUInt64)]
    [InlineData("{ let a = 1.0F; a--; }", BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.Float, SpecialType.ClrFloat)]
    [InlineData("{ let a = 1.0; a--; }", BoundUnaryOperatorKind.PostfixDecrement | BoundUnaryOperatorKind.Double, SpecialType.ClrDouble)]
    public void TestBindUnaryExpressionWithSupportedSpecialTypes(string input, BoundUnaryOperatorKind expectedOperatorKind, SpecialType expectedSpecialType)
    {
        var boundBlockStatement = TestUtils.BindStatement<BoundBlockStatement>(input);
        var boundExpressionStatement = boundBlockStatement.Statements[^1].As<BoundExpressionStatement>();
        var boundUnaryExpression = boundExpressionStatement.Expression.As<BoundUnaryExpression>();

        boundUnaryExpression.Should().NotBeNull();
        boundUnaryExpression.GetDiagnostics().Should().BeEmpty();
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
    [InlineData("{ let a = \"abc\"; ++a; }", "++", typeof(string))]
    public void TestBindUnaryExpressionWithMismatchedSpecialTypes(string input, string operatorText, Type operandType)
    {
        var boundBlockStatement = TestUtils.BindStatement<BoundBlockStatement>(input);
        var boundExpressionStatement = boundBlockStatement.Statements[^1].As<BoundExpressionStatement>();
        var boundUnaryExpression = boundExpressionStatement.Expression.As<BoundUnaryExpression>();

        boundUnaryExpression.Should().NotBeNull();
        boundUnaryExpression.GetDiagnostics().Should().NotBeEmpty();

        var diagnostic = boundUnaryExpression.GetDiagnostics().First();
        diagnostic.Message.Should().Be($"Unary operator \"{operatorText}\" is not supported on type \"{operandType.FullName}\"");
    }
}
