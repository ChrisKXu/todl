using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class InvocationExpressionTests
{
    private void PerformBasicValidationForInvocationExpression(InvocationExpression invocationExpression)
    {
        invocationExpression.Expression.Should().BeOfType<MemberAccessExpression>();

        invocationExpression.Arguments.OpenParenthesisToken.Text.ToString().Should().Be("(");
        invocationExpression.Arguments.OpenParenthesisToken.Kind.Should().Be(SyntaxKind.OpenParenthesisToken);
        invocationExpression.Arguments.CloseParenthesisToken.Text.ToString().Should().Be(")");
        invocationExpression.Arguments.CloseParenthesisToken.Kind.Should().Be(SyntaxKind.CloseParenthesisToken);
    }

    [Fact]
    public void TestParseInvocationExpressionWithoutArguments()
    {
        var inputText = "a.ToString()";
        var invocationExpression = TestUtils.ParseExpression<InvocationExpression>(inputText);

        PerformBasicValidationForInvocationExpression(invocationExpression);

        var memberAccessExpression = invocationExpression.Expression.As<MemberAccessExpression>();
        memberAccessExpression.BaseExpression.As<SimpleNameExpression>().GetText().Should().Be("a");
        memberAccessExpression.MemberIdentifierToken.Text.ToString().Should().Be("ToString");
        invocationExpression.Arguments.Items.Should().BeEmpty();
    }

    [Fact]
    public void TestParseInvocationExpressionWithOnePositionalArgument()
    {
        var inputText = "System::Int32.Parse(\"123\")";
        var invocationExpression = TestUtils.ParseExpression<InvocationExpression>(inputText);

        PerformBasicValidationForInvocationExpression(invocationExpression);

        var memberAccessExpression = invocationExpression.Expression.As<MemberAccessExpression>();
        memberAccessExpression.BaseExpression.As<NamespaceQualifiedNameExpression>().GetText().Should().Be("System::Int32");
        memberAccessExpression.MemberIdentifierToken.Text.ToString().Should().Be("Parse");

        invocationExpression.Arguments.Items.Should().NotBeEmpty();
        invocationExpression.Arguments.Items.Should().SatisfyRespectively(argument =>
        {
            argument.IsNamedArgument.Should().Be(false);
            argument.Expression.As<LiteralExpression>().GetText().Should().Be("\"123\"");
        });
    }

    [Fact]
    public void TestParseInvocationExpressionWithOneNamedArgument()
    {
        var inputText = "System::Int32.Parse(s: \"123\")";
        var invocationExpression = TestUtils.ParseExpression<InvocationExpression>(inputText);

        PerformBasicValidationForInvocationExpression(invocationExpression);

        var memberAccessExpression = invocationExpression.Expression.As<MemberAccessExpression>();
        memberAccessExpression.BaseExpression.As<NamespaceQualifiedNameExpression>().GetText().Should().Be("System::Int32");
        memberAccessExpression.MemberIdentifierToken.Text.ToString().Should().Be("Parse");

        invocationExpression.Arguments.Items.Should().NotBeEmpty();
        invocationExpression.Arguments.Items.Should().SatisfyRespectively(argument =>
        {
            argument.IsNamedArgument.Should().Be(true);
            argument.Identifier?.Text.ToString().Should().Be("s");
            argument.Identifier?.Kind.Should().Be(SyntaxKind.IdentifierToken);
            argument.ColonToken?.Text.ToString().Should().Be(":");
            argument.ColonToken?.Kind.Should().Be(SyntaxKind.ColonToken);
            argument.Expression.As<LiteralExpression>().GetText().Should().Be("\"123\"");
        });
    }

    [Fact]
    public void TestParseInvocationExpressionWithoutClosingBracket()
    {
        var inputText = "System::Int32.Parse(s: \"123\"";
        var invocationExpression = TestUtils.ParseExpression<InvocationExpression>(inputText);

        invocationExpression.Arguments.CloseParenthesisToken.Missing.Should().BeTrue();
        //invocationExpression.GetDiagnostics().Should().NotBeEmpty();
    }

    [Fact]
    public void TestParseInvocationExpressionWithMultiplePositionalArguments()
    {
        var inputText = "string.Format(\"{0} {1} {2}\", a, b, c)";
        var invocationExpression = TestUtils.ParseExpression<InvocationExpression>(inputText);

        PerformBasicValidationForInvocationExpression(invocationExpression);

        invocationExpression.Expression.As<MemberAccessExpression>().MemberIdentifierToken.Text.ToString().Should().Be("Format");
        invocationExpression.Arguments.Items.Should().HaveCount(4);

        invocationExpression.Arguments.Items.Should().SatisfyRespectively(
            arg0 =>
            {
                arg0.IsNamedArgument.Should().BeFalse();
                arg0.Expression.Should().BeOfType<LiteralExpression>();
            },
            arg1 =>
            {
                arg1.IsNamedArgument.Should().BeFalse();
                arg1.Expression.As<SimpleNameExpression>().GetText().Should().Be("a");
            },
            arg2 =>
            {
                arg2.IsNamedArgument.Should().BeFalse();
                arg2.Expression.As<SimpleNameExpression>().GetText().Should().Be("b");
            },
            arg3 =>
            {
                arg3.IsNamedArgument.Should().BeFalse();
                arg3.Expression.As<SimpleNameExpression>().GetText().Should().Be("c");
            });
    }

    [Fact]
    public void TestParseInvocationExpressionWithMultipleNamedArguments()
    {
        var inputText = "obj.Method(first: 1, second: 2, third: 3)";
        var invocationExpression = TestUtils.ParseExpression<InvocationExpression>(inputText);

        PerformBasicValidationForInvocationExpression(invocationExpression);

        invocationExpression.Expression.As<MemberAccessExpression>().MemberIdentifierToken.Text.ToString().Should().Be("Method");
        invocationExpression.Arguments.Items.Should().HaveCount(3);

        invocationExpression.Arguments.Items.Should().SatisfyRespectively(
            arg0 =>
            {
                arg0.IsNamedArgument.Should().BeTrue();
                arg0.Identifier?.Text.ToString().Should().Be("first");
            },
            arg1 =>
            {
                arg1.IsNamedArgument.Should().BeTrue();
                arg1.Identifier?.Text.ToString().Should().Be("second");
            },
            arg2 =>
            {
                arg2.IsNamedArgument.Should().BeTrue();
                arg2.Identifier?.Text.ToString().Should().Be("third");
            });
    }

    [Fact]
    public void TestInvocationWithExpressionArguments()
    {
        var inputText = "Math.Max(a + b, c * d)";
        var invocationExpression = TestUtils.ParseExpression<InvocationExpression>(inputText);

        invocationExpression.Should().NotBeNull();
        invocationExpression.Expression.As<MemberAccessExpression>().MemberIdentifierToken.Text.ToString().Should().Be("Max");
        invocationExpression.Arguments.Items.Should().HaveCount(2);

        invocationExpression.Arguments.Items[0].Expression.Should().BeOfType<BinaryExpression>();
        invocationExpression.Arguments.Items[1].Expression.Should().BeOfType<BinaryExpression>();
    }

    [Fact]
    public void TestChainedMethodCalls()
    {
        var inputText = "a.ToString().ToUpper()";
        var invocationExpression = TestUtils.ParseExpression<InvocationExpression>(inputText);

        invocationExpression.Should().NotBeNull();
        var outerMemberAccess = invocationExpression.Expression.As<MemberAccessExpression>();
        outerMemberAccess.MemberIdentifierToken.Text.ToString().Should().Be("ToUpper");
        invocationExpression.Arguments.Items.Should().BeEmpty();

        var innerCall = outerMemberAccess.BaseExpression.As<InvocationExpression>();
        innerCall.Should().NotBeNull();
        var innerMemberAccess = innerCall.Expression.As<MemberAccessExpression>();
        innerMemberAccess.MemberIdentifierToken.Text.ToString().Should().Be("ToString");
        innerMemberAccess.BaseExpression.As<SimpleNameExpression>().GetText().Should().Be("a");
    }

    [Fact]
    public void TestDeeplyChainedMethodCalls()
    {
        var inputText = "a.First().Second().Third()";
        var invocationExpression = TestUtils.ParseExpression<InvocationExpression>(inputText);

        invocationExpression.Should().NotBeNull();
        invocationExpression.Expression.As<MemberAccessExpression>().MemberIdentifierToken.Text.ToString().Should().Be("Third");

        var second = invocationExpression.Expression.As<MemberAccessExpression>().BaseExpression.As<InvocationExpression>();
        second.Expression.As<MemberAccessExpression>().MemberIdentifierToken.Text.ToString().Should().Be("Second");

        var first = second.Expression.As<MemberAccessExpression>().BaseExpression.As<InvocationExpression>();
        first.Expression.As<MemberAccessExpression>().MemberIdentifierToken.Text.ToString().Should().Be("First");

        first.Expression.As<MemberAccessExpression>().BaseExpression.As<SimpleNameExpression>().GetText().Should().Be("a");
    }

    [Fact]
    public void TestInvocationWithNestedInvocationArgument()
    {
        var inputText = "outer.Call(inner.GetValue())";
        var invocationExpression = TestUtils.ParseExpression<InvocationExpression>(inputText);

        invocationExpression.Should().NotBeNull();
        invocationExpression.Expression.As<MemberAccessExpression>().MemberIdentifierToken.Text.ToString().Should().Be("Call");
        invocationExpression.Arguments.Items.Should().HaveCount(1);

        var innerCall = invocationExpression.Arguments.Items[0].Expression.As<InvocationExpression>();
        innerCall.Expression.As<MemberAccessExpression>().MemberIdentifierToken.Text.ToString().Should().Be("GetValue");
    }

    [Fact]
    public void TestInvocationOnQualifiedType()
    {
        var inputText = "System::Math.Abs(-5)";
        var invocationExpression = TestUtils.ParseExpression<InvocationExpression>(inputText);

        invocationExpression.Should().NotBeNull();
        var memberAccessExpression = invocationExpression.Expression.As<MemberAccessExpression>();
        memberAccessExpression.MemberIdentifierToken.Text.ToString().Should().Be("Abs");
        memberAccessExpression.BaseExpression.Should().BeOfType<NamespaceQualifiedNameExpression>();
        invocationExpression.Arguments.Items.Should().HaveCount(1);
    }

    [Fact]
    public void TestMixedPositionalAndNamedArgumentsProducesError()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var inputText = "obj.Method(1, named: 2)";
        var invocationExpression = TestUtils.ParseExpression<InvocationExpression>(inputText, diagnosticBuilder);

        invocationExpression.Should().NotBeNull();
        invocationExpression.Arguments.Items.Should().HaveCount(2);

        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().NotBeEmpty();
        diagnostics.Should().Contain(d => d.ErrorCode == ErrorCode.MixedPositionalAndNamedArguments);
    }
}
