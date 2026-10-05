using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class NewArrayExpressionTests
{
    [Fact]
    public void TestParseNewArrayExpressionBasic()
    {
        var inputText = "new int[5]";
        var newArrayExpression = TestUtils.ParseExpression<NewArrayExpression>(inputText);

        newArrayExpression.NewKeywordToken.Kind.Should().Be(SyntaxKind.NewKeywordToken);
        newArrayExpression.ElementTypeNameExpression.GetText().Should().Be("int");
        newArrayExpression.LengthExpression.GetText().Should().Be("5");
        newArrayExpression.ArrayRankSpecifiers.Should().BeEmpty();
    }

    [Fact]
    public void TestParseNewArrayExpressionWithQualifiedElementType()
    {
        var inputText = "new System::Int32[5]";
        var newArrayExpression = TestUtils.ParseExpression<NewArrayExpression>(inputText);

        newArrayExpression.ElementTypeNameExpression.GetText().Should().Be("System::Int32");
        newArrayExpression.LengthExpression.GetText().Should().Be("5");
    }

    [Fact]
    public void TestParseNewArrayExpressionWithComplexLengthExpression()
    {
        var inputText = "new int[n + 1]";
        var newArrayExpression = TestUtils.ParseExpression<NewArrayExpression>(inputText);

        newArrayExpression.LengthExpression.Should().BeOfType<BinaryExpression>();
        newArrayExpression.LengthExpression.GetText().Should().Be("n + 1");
    }

    [Fact]
    public void TestParseJaggedNewArrayExpression()
    {
        var inputText = "new int[3][]";
        var newArrayExpression = TestUtils.ParseExpression<NewArrayExpression>(inputText);

        newArrayExpression.ElementTypeNameExpression.GetText().Should().Be("int");
        newArrayExpression.LengthExpression.GetText().Should().Be("3");
        newArrayExpression.ArrayRankSpecifiers.Should().HaveCount(1);
    }

    [Fact]
    public void TestParseObjectCreationStillParsesAsNewExpression()
    {
        var inputText = "new System::Exception()";
        var newExpression = TestUtils.ParseExpression<NewExpression>(inputText);

        newExpression.TypeNameExpression.GetText().Should().Be("System::Exception");
    }

    [Fact]
    public void TestParseNewArrayExpressionIsPrimaryExpressionReceiver()
    {
        var inputText = "(new int[3]).Length";
        var memberAccessExpression = TestUtils.ParseExpression<MemberAccessExpression>(inputText);

        memberAccessExpression.BaseExpression.Should().BeOfType<ParethesizedExpression>();
        memberAccessExpression.MemberIdentifierToken.Text.ToString().Should().Be("Length");
    }
}
