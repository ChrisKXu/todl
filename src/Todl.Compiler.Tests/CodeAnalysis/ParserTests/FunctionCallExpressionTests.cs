using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class FunctionCallExpressionTests
{
    private void PerformBasicValidationForFunctionCallExpression(FunctionCallExpression functionCallExpression)
    {
        functionCallExpression.DotToken.Text.Should().Be(".");
        functionCallExpression.DotToken.Kind.Should().Be(SyntaxKind.DotToken);
        functionCallExpression.NameToken.Kind.Should().Be(SyntaxKind.IdentifierToken);

        functionCallExpression.Arguments.OpenParenthesisToken.Text.Should().Be("(");
        functionCallExpression.Arguments.OpenParenthesisToken.Kind.Should().Be(SyntaxKind.OpenParenthesisToken);
        functionCallExpression.Arguments.CloseParenthesisToken.Text.Should().Be(")");
        functionCallExpression.Arguments.CloseParenthesisToken.Kind.Should().Be(SyntaxKind.CloseParenthesisToken);
    }

    [Fact]
    public void TestParseFunctionCallExpressionWithoutArguments()
    {
        var inputText = "a.ToString()";
        var functionCallExpression = TestUtils.ParseExpression<FunctionCallExpression>(inputText);

        PerformBasicValidationForFunctionCallExpression(functionCallExpression);

        functionCallExpression.BaseExpression.As<SimpleNameExpression>().Text.Should().Be("a");
        functionCallExpression.NameToken.Text.Should().Be("ToString");
        functionCallExpression.Arguments.Items.Should().BeEmpty();
    }

    [Fact]
    public void TestParseFunctionCallExpressionWithOnePositionalArgument()
    {
        var inputText = "System::Int32.Parse(\"123\")";
        var functionCallExpression = TestUtils.ParseExpression<FunctionCallExpression>(inputText);

        PerformBasicValidationForFunctionCallExpression(functionCallExpression);

        functionCallExpression.BaseExpression.As<NamespaceQualifiedNameExpression>().Text.Should().Be("System::Int32");
        functionCallExpression.NameToken.Text.Should().Be("Parse");

        functionCallExpression.Arguments.Items.Should().NotBeEmpty();
        functionCallExpression.Arguments.Items.Should().SatisfyRespectively(argument =>
        {
            argument.IsNamedArgument.Should().Be(false);
            argument.Expression.As<LiteralExpression>().Text.Should().Be("\"123\"");
        });
    }

    [Fact]
    public void TestParseFunctionCallExpressionWithOneNamedArgument()
    {
        var inputText = "System::Int32.Parse(s: \"123\")";
        var functionCallExpression = TestUtils.ParseExpression<FunctionCallExpression>(inputText);

        PerformBasicValidationForFunctionCallExpression(functionCallExpression);

        functionCallExpression.BaseExpression.As<NamespaceQualifiedNameExpression>().Text.Should().Be("System::Int32");
        functionCallExpression.NameToken.Text.Should().Be("Parse");

        functionCallExpression.Arguments.Items.Should().NotBeEmpty();
        functionCallExpression.Arguments.Items.Should().SatisfyRespectively(argument =>
        {
            argument.IsNamedArgument.Should().Be(true);
            argument.Identifier?.Text.Should().Be("s");
            argument.Identifier?.Kind.Should().Be(SyntaxKind.IdentifierToken);
            argument.ColonToken?.Text.Should().Be(":");
            argument.ColonToken?.Kind.Should().Be(SyntaxKind.ColonToken);
            argument.Expression.As<LiteralExpression>().Text.Should().Be("\"123\"");
        });
    }

    [Fact]
    public void TestParseFunctionCallExpressionWithoutClosingBracket()
    {
        var inputText = "System::Int32.Parse(s: \"123\"";
        var functionCallExpression = TestUtils.ParseExpression<FunctionCallExpression>(inputText);

        functionCallExpression.Arguments.CloseParenthesisToken.Missing.Should().BeTrue();
        //functionCallExpression.GetDiagnostics().Should().NotBeEmpty();
    }

    [Fact]
    public void TestParseFunctionCallExpressionWithMultiplePositionalArguments()
    {
        var inputText = "string.Format(\"{0} {1} {2}\", a, b, c)";
        var functionCallExpression = TestUtils.ParseExpression<FunctionCallExpression>(inputText);

        PerformBasicValidationForFunctionCallExpression(functionCallExpression);

        functionCallExpression.NameToken.Text.Should().Be("Format");
        functionCallExpression.Arguments.Items.Should().HaveCount(4);

        functionCallExpression.Arguments.Items.Should().SatisfyRespectively(
            arg0 =>
            {
                arg0.IsNamedArgument.Should().BeFalse();
                arg0.Expression.Should().BeOfType<LiteralExpression>();
            },
            arg1 =>
            {
                arg1.IsNamedArgument.Should().BeFalse();
                arg1.Expression.As<SimpleNameExpression>().Text.Should().Be("a");
            },
            arg2 =>
            {
                arg2.IsNamedArgument.Should().BeFalse();
                arg2.Expression.As<SimpleNameExpression>().Text.Should().Be("b");
            },
            arg3 =>
            {
                arg3.IsNamedArgument.Should().BeFalse();
                arg3.Expression.As<SimpleNameExpression>().Text.Should().Be("c");
            });
    }

    [Fact]
    public void TestParseFunctionCallExpressionWithMultipleNamedArguments()
    {
        var inputText = "obj.Method(first: 1, second: 2, third: 3)";
        var functionCallExpression = TestUtils.ParseExpression<FunctionCallExpression>(inputText);

        PerformBasicValidationForFunctionCallExpression(functionCallExpression);

        functionCallExpression.NameToken.Text.Should().Be("Method");
        functionCallExpression.Arguments.Items.Should().HaveCount(3);

        functionCallExpression.Arguments.Items.Should().SatisfyRespectively(
            arg0 =>
            {
                arg0.IsNamedArgument.Should().BeTrue();
                arg0.Identifier?.Text.Should().Be("first");
            },
            arg1 =>
            {
                arg1.IsNamedArgument.Should().BeTrue();
                arg1.Identifier?.Text.Should().Be("second");
            },
            arg2 =>
            {
                arg2.IsNamedArgument.Should().BeTrue();
                arg2.Identifier?.Text.Should().Be("third");
            });
    }

    [Fact]
    public void TestFunctionCallWithExpressionArguments()
    {
        var inputText = "Math.Max(a + b, c * d)";
        var functionCallExpression = TestUtils.ParseExpression<FunctionCallExpression>(inputText);

        functionCallExpression.Should().NotBeNull();
        functionCallExpression.NameToken.Text.Should().Be("Max");
        functionCallExpression.Arguments.Items.Should().HaveCount(2);

        functionCallExpression.Arguments.Items[0].Expression.Should().BeOfType<BinaryExpression>();
        functionCallExpression.Arguments.Items[1].Expression.Should().BeOfType<BinaryExpression>();
    }

    [Fact]
    public void TestChainedMethodCalls()
    {
        var inputText = "a.ToString().ToUpper()";
        var functionCallExpression = TestUtils.ParseExpression<FunctionCallExpression>(inputText);

        functionCallExpression.Should().NotBeNull();
        functionCallExpression.NameToken.Text.Should().Be("ToUpper");
        functionCallExpression.Arguments.Items.Should().BeEmpty();

        var innerCall = functionCallExpression.BaseExpression.As<FunctionCallExpression>();
        innerCall.Should().NotBeNull();
        innerCall.NameToken.Text.Should().Be("ToString");
        innerCall.BaseExpression.As<SimpleNameExpression>().Text.Should().Be("a");
    }

    [Fact]
    public void TestDeeplyChainedMethodCalls()
    {
        var inputText = "a.First().Second().Third()";
        var functionCallExpression = TestUtils.ParseExpression<FunctionCallExpression>(inputText);

        functionCallExpression.Should().NotBeNull();
        functionCallExpression.NameToken.Text.Should().Be("Third");

        var second = functionCallExpression.BaseExpression.As<FunctionCallExpression>();
        second.NameToken.Text.Should().Be("Second");

        var first = second.BaseExpression.As<FunctionCallExpression>();
        first.NameToken.Text.Should().Be("First");

        first.BaseExpression.As<SimpleNameExpression>().Text.Should().Be("a");
    }

    [Fact]
    public void TestFunctionCallWithNestedFunctionCallArgument()
    {
        var inputText = "outer.Call(inner.GetValue())";
        var functionCallExpression = TestUtils.ParseExpression<FunctionCallExpression>(inputText);

        functionCallExpression.Should().NotBeNull();
        functionCallExpression.NameToken.Text.Should().Be("Call");
        functionCallExpression.Arguments.Items.Should().HaveCount(1);

        var innerCall = functionCallExpression.Arguments.Items[0].Expression.As<FunctionCallExpression>();
        innerCall.NameToken.Text.Should().Be("GetValue");
    }

    [Fact]
    public void TestFunctionCallOnQualifiedType()
    {
        var inputText = "System::Math.Abs(-5)";
        var functionCallExpression = TestUtils.ParseExpression<FunctionCallExpression>(inputText);

        functionCallExpression.Should().NotBeNull();
        functionCallExpression.NameToken.Text.Should().Be("Abs");
        functionCallExpression.BaseExpression.Should().BeOfType<NamespaceQualifiedNameExpression>();
        functionCallExpression.Arguments.Items.Should().HaveCount(1);
    }

    [Fact]
    public void TestMixedPositionalAndNamedArgumentsProducesError()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var inputText = "obj.Method(1, named: 2)";
        var functionCallExpression = TestUtils.ParseExpression<FunctionCallExpression>(inputText, diagnosticBuilder);

        functionCallExpression.Should().NotBeNull();
        functionCallExpression.Arguments.Items.Should().HaveCount(2);

        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().NotBeEmpty();
        diagnostics.Should().Contain(d => d.ErrorCode == ErrorCode.MixedPositionalAndNamedArguments);
    }
}
