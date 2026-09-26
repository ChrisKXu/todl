using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundInvocationExpressionTests
{
    [Fact]
    public void TestBindClrInvocationExpressionWithNoArguments()
    {
        var boundInvocationExpression = TestUtils.BindExpression<BoundClrInvocationExpression>("100.ToString()");

        boundInvocationExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrString);
        boundInvocationExpression.MethodInfo.Name.Should().Be("ToString");
        boundInvocationExpression.IsStatic.Should().Be(false);
    }

    [Fact]
    public void TestBindClrInvocationExpressionWithOnePositionalArgument()
    {
        var boundInvocationExpression = TestUtils.BindExpression<BoundClrInvocationExpression>("System::Math.Abs(-10)");

        boundInvocationExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrInt32);
        boundInvocationExpression.MethodInfo.Name.Should().Be("Abs");
        boundInvocationExpression.IsStatic.Should().Be(true);
        boundInvocationExpression.BoundArguments.Should().HaveCount(1);

        var argument = boundInvocationExpression.BoundArguments[0].As<BoundUnaryExpression>();
        argument.Operator.BoundUnaryOperatorKind.Should().Be(BoundUnaryOperatorKind.UnaryMinus | BoundUnaryOperatorKind.Int);
        argument.Operand.As<BoundConstant>().Value.Should().Be(10);
    }

    [Fact]
    public void TestBindClrInvocationExpressionWithOneNamedArgument()
    {
        var boundInvocationExpression = TestUtils.BindExpression<BoundClrInvocationExpression>("100.ToString(format: \"G\")");

        boundInvocationExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrString);
        boundInvocationExpression.MethodInfo.Name.Should().Be("ToString");
        boundInvocationExpression.IsStatic.Should().Be(false);
        boundInvocationExpression.BoundArguments.Should().HaveCount(1);

        var argument = boundInvocationExpression.BoundArguments[0].As<BoundConstant>();
        argument.Value.Should().Be("G");
    }

    [Fact]
    public void TestBindClrInvocationExpressionWithMultiplePositionalArguments()
    {
        var boundInvocationExpression = TestUtils.BindExpression<BoundClrInvocationExpression>("\"abcde\".IndexOf(\"ab\", 1, 2)");

        boundInvocationExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrInt32);
        boundInvocationExpression.MethodInfo.Name.Should().Be("IndexOf");
        boundInvocationExpression.IsStatic.Should().Be(false);

        var boundArguments = boundInvocationExpression.BoundArguments;
        boundArguments.Should().HaveCount(3);
        boundArguments[0].As<BoundConstant>().Value.Should().Be("ab");
        boundArguments[1].As<BoundConstant>().Value.Should().Be(1);
        boundArguments[2].As<BoundConstant>().Value.Should().Be(2);
    }

    [Theory]
    [InlineData("\"abcde\".Substring(startIndex: 1, length: 2)")]
    [InlineData("\"abcde\".Substring(length: 2, startIndex: 1)")]
    public void TestBindClrInvocationExpressionWithMultipleNamedArguments(string inputText)
    {
        var boundInvocationExpression = TestUtils.BindExpression<BoundClrInvocationExpression>(inputText);

        boundInvocationExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrString);
        boundInvocationExpression.MethodInfo.Name.Should().Be("Substring");
        boundInvocationExpression.IsStatic.Should().Be(false);

        var boundArguments = boundInvocationExpression.BoundArguments;
        boundArguments.Should().HaveCount(2);
        boundArguments[0].As<BoundConstant>().Value.Should().Be(1);
        boundArguments[1].As<BoundConstant>().Value.Should().Be(2);
    }

    [Fact]
    public void TestBindClrInvocationExpressionWithImportDirective()
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
    public void BindInvocationWithNoMatchingPositionalOverloadShouldReportDiagnosticAndReturnInvalidNode()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundExpression = TestUtils.BindExpression<BoundExpression>("100.ToString(1, 2, 3)", diagnosticBuilder);

        boundExpression.Should().BeOfType<BoundInvalidInvocationExpression>();
        boundExpression.ResultType.Should().BeNull();

        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().ContainSingle();
        diagnostics.Single().Level.Should().Be(DiagnosticLevel.Error);
        diagnostics.Single().ErrorCode.Should().Be(ErrorCode.NoMatchingCandidate);
    }

    [Fact]
    public void BindInvocationWithNoMatchingNamedOverloadShouldReportDiagnosticAndReturnInvalidNode()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundExpression = TestUtils.BindExpression<BoundExpression>("100.ToString(format: \"G\", bogus: 1)", diagnosticBuilder);

        boundExpression.Should().BeOfType<BoundInvalidInvocationExpression>();
        boundExpression.ResultType.Should().BeNull();

        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().ContainSingle();
        diagnostics.Single().Level.Should().Be(DiagnosticLevel.Error);
        diagnostics.Single().ErrorCode.Should().Be(ErrorCode.NoMatchingCandidate);
    }

    [Fact]
    public void BindInvocationOnNonInvocableExpressionShouldReportDiagnosticAndReturnInvalidNode()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundExpression = TestUtils.BindExpression<BoundExpression>("\"abc\"(1)", diagnosticBuilder);

        boundExpression.Should().BeOfType<BoundInvalidInvocationExpression>();

        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().ContainSingle();
        diagnostics.Single().Level.Should().Be(DiagnosticLevel.Error);
        diagnostics.Single().ErrorCode.Should().Be(ErrorCode.ExpressionNotInvocable);
    }

    [Fact]
    public void TestBindTodlInvocationExpressionWithNoArguments()
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
    public void TestBindTodlInvocationExpressionWithOneNamedArguments()
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
    public void TestBindTodlInvocationExpressionWithMultipleNamedArguments()
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
