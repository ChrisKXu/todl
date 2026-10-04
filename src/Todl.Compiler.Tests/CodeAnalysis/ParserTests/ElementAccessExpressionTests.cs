using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class ElementAccessExpressionTests
{
    [Fact]
    public void TestParseElementAccessExpressionBasic()
    {
        var inputText = "a[0]";
        var elementAccessExpression = TestUtils.ParseExpression<ElementAccessExpression>(inputText);

        elementAccessExpression.BaseExpression.GetText().Should().Be("a");
        elementAccessExpression.IndexExpression.GetText().Should().Be("0");
    }

    [Fact]
    public void TestParseChainedElementAccessExpression()
    {
        var inputText = "a[0][1]";
        var elementAccessExpression = TestUtils.ParseExpression<ElementAccessExpression>(inputText);

        elementAccessExpression.IndexExpression.GetText().Should().Be("1");
        elementAccessExpression.BaseExpression.Should().BeOfType<ElementAccessExpression>();

        var innerElementAccessExpression = elementAccessExpression.BaseExpression.As<ElementAccessExpression>();
        innerElementAccessExpression.BaseExpression.GetText().Should().Be("a");
        innerElementAccessExpression.IndexExpression.GetText().Should().Be("0");
    }

    [Fact]
    public void TestParseElementAccessAfterMemberAccess()
    {
        var inputText = "a.b[0]";
        var elementAccessExpression = TestUtils.ParseExpression<ElementAccessExpression>(inputText);

        elementAccessExpression.BaseExpression.Should().BeOfType<MemberAccessExpression>();
        elementAccessExpression.BaseExpression.GetText().Should().Be("a.b");
    }

    [Fact]
    public void TestParseElementAccessAfterInvocation()
    {
        var inputText = "f()[0]";
        var elementAccessExpression = TestUtils.ParseExpression<ElementAccessExpression>(inputText);

        elementAccessExpression.BaseExpression.Should().BeOfType<InvocationExpression>();
    }

    [Fact]
    public void TestParseElementAccessWithComplexIndexExpression()
    {
        var inputText = "a[i + 1]";
        var elementAccessExpression = TestUtils.ParseExpression<ElementAccessExpression>(inputText);

        elementAccessExpression.IndexExpression.Should().BeOfType<BinaryExpression>();
    }

    [Fact]
    public void TestParseElementAccessAsAssignmentTarget()
    {
        var inputText = "a[0] = 1";
        var assignmentExpression = TestUtils.ParseExpression<AssignmentExpression>(inputText);

        assignmentExpression.Left.Should().BeOfType<ElementAccessExpression>();
    }
}
