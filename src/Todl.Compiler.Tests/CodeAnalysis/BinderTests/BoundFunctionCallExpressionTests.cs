﻿using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundFunctionCallExpressionTests
{
    [Fact]
    public void TestBindClrFunctionCallExpressionWithNoArguments()
    {
        var boundFunctionCallExpression = TestUtils.BindExpression<BoundClrFunctionCallExpression>("100.ToString()");

        boundFunctionCallExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrString);
        boundFunctionCallExpression.MethodInfo.Name.Should().Be("ToString");
        boundFunctionCallExpression.IsStatic.Should().Be(false);
    }

    [Fact]
    public void TestBindClrFunctionCallExpressionWithOnePositionalArgument()
    {
        var boundFunctionCallExpression = TestUtils.BindExpression<BoundClrFunctionCallExpression>("System.Math.Abs(-10)");

        boundFunctionCallExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrInt32);
        boundFunctionCallExpression.MethodInfo.Name.Should().Be("Abs");
        boundFunctionCallExpression.IsStatic.Should().Be(true);
        boundFunctionCallExpression.BoundArguments.Should().HaveCount(1);

        var argument = boundFunctionCallExpression.BoundArguments[0].As<BoundUnaryExpression>();
        argument.Operator.BoundUnaryOperatorKind.Should().Be(BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Int);
        argument.Operand.As<BoundConstant>().Value.Should().Be(10);
    }

    [Fact]
    public void TestBindClrFunctionCallExpressionWithOneNamedArgument()
    {
        var boundFunctionCallExpression = TestUtils.BindExpression<BoundClrFunctionCallExpression>("100.ToString(format: \"G\")");

        boundFunctionCallExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrString);
        boundFunctionCallExpression.MethodInfo.Name.Should().Be("ToString");
        boundFunctionCallExpression.IsStatic.Should().Be(false);
        boundFunctionCallExpression.BoundArguments.Should().HaveCount(1);

        var argument = boundFunctionCallExpression.BoundArguments[0].As<BoundConstant>();
        argument.Value.Should().Be("G");
    }

    [Fact]
    public void TestBindClrFunctionCallExpressionWithMultiplePositionalArguments()
    {
        var boundFunctionCallExpression = TestUtils.BindExpression<BoundClrFunctionCallExpression>("\"abcde\".IndexOf(\"ab\", 1, 2)");

        boundFunctionCallExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrInt32);
        boundFunctionCallExpression.MethodInfo.Name.Should().Be("IndexOf");
        boundFunctionCallExpression.IsStatic.Should().Be(false);

        var boundArguments = boundFunctionCallExpression.BoundArguments;
        boundArguments.Should().HaveCount(3);
        boundArguments[0].As<BoundConstant>().Value.Should().Be("ab");
        boundArguments[1].As<BoundConstant>().Value.Should().Be(1);
        boundArguments[2].As<BoundConstant>().Value.Should().Be(2);
    }

    [Theory]
    [InlineData("\"abcde\".Substring(startIndex: 1, length: 2)")]
    [InlineData("\"abcde\".Substring(length: 2, startIndex: 1)")]
    public void TestBindClrFunctionCallExpressionWithMultipleNamedArguments(string inputText)
    {
        var boundFunctionCallExpression = TestUtils.BindExpression<BoundClrFunctionCallExpression>(inputText);

        boundFunctionCallExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrString);
        boundFunctionCallExpression.MethodInfo.Name.Should().Be("Substring");
        boundFunctionCallExpression.IsStatic.Should().Be(false);

        var boundArguments = boundFunctionCallExpression.BoundArguments;
        boundArguments.Should().HaveCount(2);
        boundArguments[0].As<BoundConstant>().Value.Should().Be(1);
        boundArguments[1].As<BoundConstant>().Value.Should().Be(2);
    }

    [Fact]
    public void TestBindClrFunctionCallExpressionWithImportDirective()
    {
        var inputText = @"
            import { Console } from System;

            void Main() {
                Console.WriteLine();
            }
        ";

        TestUtils.BindModule(inputText).Should().NotBeNull();
    }

    [Fact]
    public void TestBindTodlFunctionCallExpressionWithNoArguments()
    {
        var inputText = @"
            int func() {
                return 20;
            }

            int Main() {
                const a = func();
                a.ToString();
                return 0;
            }
        ";

        TestUtils.BindModule(inputText).Should().NotBeNull();
    }

    [Fact]
    public void TestBindTodlFunctionCallExpressionWithOneNamedArguments()
    {
        var inputText = @"
            int func(int input) {
                return input;
            }

            int Main() {
                const a = func(input: 20);
                a.ToString();
                return 0;
            }
        ";

        TestUtils.BindModule(inputText).Should().NotBeNull();
    }

    [Fact]
    public void TestBindTodlFunctionCallExpressionWithMultipleNamedArguments()
    {
        var inputText = @"
            int func(int a, string b) {
                return a + b.Length;
            }

            int Main() {
                const a = func(a: 20, b: string.Empty);
                const b = func(b: string.Empty, a: 20);
                a.ToString();
                b.ToString();
                return 0;
            }
        ";

        TestUtils.BindModule(inputText).Should().NotBeNull();
    }
}
