using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed partial class BinderTests
{
    [Fact]
    public void TestBindClrFunctionCallExpressionWithNoArguments()
    {
        var boundFunctionCallExpression = BindExpression<BoundClrFunctionCallExpression>("100.ToString()");

        boundFunctionCallExpression.ResultType.Should().Be(builtInTypes.String);
        boundFunctionCallExpression.MethodInfo.Name.Should().Be("ToString");
        boundFunctionCallExpression.IsStatic.Should().Be(false);
    }

    [Fact]
    public void TestBindClrFunctionCallExpressionWithOnePositionalArgument()
    {
        var boundFunctionCallExpression = BindExpression<BoundClrFunctionCallExpression>("System.Math.Abs(-10)");

        boundFunctionCallExpression.ResultType.Should().Be(builtInTypes.Int32);
        boundFunctionCallExpression.MethodInfo.Name.Should().Be("Abs");
        boundFunctionCallExpression.IsStatic.Should().Be(true);
        boundFunctionCallExpression.BoundArguments.Count.Should().Be(1);

        var argument = boundFunctionCallExpression.BoundArguments[0].As<BoundUnaryExpression>();
        argument.Operator.BoundUnaryOperatorKind.Should().Be(BoundUnaryExpression.BoundUnaryOperatorKind.Negation);
        argument.Operand.As<BoundConstant>().Value.Should().Be(10);
    }

    [Fact]
    public void TestBindClrFunctionCallExpressionWithOneNamedArgument()
    {
        var boundFunctionCallExpression = BindExpression<BoundClrFunctionCallExpression>("100.ToString(format: \"G\")");

        boundFunctionCallExpression.ResultType.Should().Be(builtInTypes.String);
        boundFunctionCallExpression.MethodInfo.Name.Should().Be("ToString");
        boundFunctionCallExpression.IsStatic.Should().Be(false);
        boundFunctionCallExpression.BoundArguments.Count.Should().Be(1);

        var argument = boundFunctionCallExpression.BoundArguments[0].As<BoundConstant>();
        argument.Value.Should().Be("G");
    }

    [Fact]
    public void TestBindClrFunctionCallExpressionWithMultiplePositionalArguments()
    {
        var boundFunctionCallExpression = BindExpression<BoundClrFunctionCallExpression>("\"abcde\".IndexOf(\"ab\", 1, 2)");

        boundFunctionCallExpression.ResultType.Should().Be(builtInTypes.String);
        boundFunctionCallExpression.MethodInfo.Name.Should().Be("IndexOf");
        boundFunctionCallExpression.IsStatic.Should().Be(false);

        var boundArguments = boundFunctionCallExpression.BoundArguments;
        boundArguments.Count.Should().Be(3);
        boundArguments[0].As<BoundConstant>().Value.Should().Be("ab");
        boundArguments[1].As<BoundConstant>().Value.Should().Be(1);
        boundArguments[2].As<BoundConstant>().Value.Should().Be(2);
    }

    [Theory]
    [InlineData("\"abcde\".Substring(startIndex: 1, length: 2)")]
    [InlineData("\"abcde\".Substring(length: 2, startIndex: 1)")]
    public void TestBindClrFunctionCallExpressionWithMultipleNamedArguments(string inputText)
    {
        var boundFunctionCallExpression = BindExpression<BoundClrFunctionCallExpression>(inputText);

        boundFunctionCallExpression.ResultType.Should().Be(builtInTypes.String);
        boundFunctionCallExpression.MethodInfo.Name.Should().Be("Substring");
        boundFunctionCallExpression.IsStatic.Should().Be(false);

        var boundArguments = boundFunctionCallExpression.BoundArguments;
        boundArguments.Count.Should().Be(2);
        boundArguments[0].As<BoundConstant>().Value.Should().Be(1);
        boundArguments[1].As<BoundConstant>().Value.Should().Be(2);
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
        var syntaxTree = ParseSyntaxTree(inputText);
        var boundModule = BoundModule.Create(new[] { syntaxTree });

        boundModule.GetDiagnostics().Should().BeEmpty();
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
        var syntaxTree = ParseSyntaxTree(inputText);
        var boundModule = BoundModule.Create(new[] { syntaxTree });

        boundModule.GetDiagnostics().Should().BeEmpty();
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
        var syntaxTree = ParseSyntaxTree(inputText);
        var boundModule = BoundModule.Create(new[] { syntaxTree });

        boundModule.GetDiagnostics().Should().BeEmpty();
    }
}
