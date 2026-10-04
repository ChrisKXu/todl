using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class ArrayCreationExpressionTests
{
    [Fact]
    public void TestParseArrayCreationExpressionBasic()
    {
        var inputText = "new int[5]";
        var arrayCreationExpression = TestUtils.ParseExpression<ArrayCreationExpression>(inputText);

        arrayCreationExpression.NewKeywordToken.Kind.Should().Be(SyntaxKind.NewKeywordToken);
        arrayCreationExpression.ElementTypeNameExpression.GetText().Should().Be("int");
        arrayCreationExpression.LengthExpression.GetText().Should().Be("5");
        arrayCreationExpression.ArrayRankSpecifiers.Should().BeEmpty();
    }

    [Fact]
    public void TestParseArrayCreationExpressionWithQualifiedElementType()
    {
        var inputText = "new System::Int32[5]";
        var arrayCreationExpression = TestUtils.ParseExpression<ArrayCreationExpression>(inputText);

        arrayCreationExpression.ElementTypeNameExpression.GetText().Should().Be("System::Int32");
        arrayCreationExpression.LengthExpression.GetText().Should().Be("5");
    }

    [Fact]
    public void TestParseArrayCreationExpressionWithComplexLengthExpression()
    {
        var inputText = "new int[n + 1]";
        var arrayCreationExpression = TestUtils.ParseExpression<ArrayCreationExpression>(inputText);

        arrayCreationExpression.LengthExpression.Should().BeOfType<BinaryExpression>();
        arrayCreationExpression.LengthExpression.GetText().Should().Be("n + 1");
    }

    [Fact]
    public void TestParseJaggedArrayCreationExpression()
    {
        var inputText = "new int[3][]";
        var arrayCreationExpression = TestUtils.ParseExpression<ArrayCreationExpression>(inputText);

        arrayCreationExpression.ElementTypeNameExpression.GetText().Should().Be("int");
        arrayCreationExpression.LengthExpression.GetText().Should().Be("3");
        arrayCreationExpression.ArrayRankSpecifiers.Should().HaveCount(1);
    }

    [Fact]
    public void TestParseObjectCreationStillParsesAsNewExpression()
    {
        var inputText = "new System::Exception()";
        var newExpression = TestUtils.ParseExpression<NewExpression>(inputText);

        newExpression.TypeNameExpression.GetText().Should().Be("System::Exception");
    }

    [Fact]
    public void TestParseArrayCreationExpressionIsPrimaryExpressionReceiver()
    {
        var inputText = "(new int[3]).Length";
        var memberAccessExpression = TestUtils.ParseExpression<MemberAccessExpression>(inputText);

        memberAccessExpression.BaseExpression.Should().BeOfType<ParethesizedExpression>();
        memberAccessExpression.MemberIdentifierToken.Text.ToString().Should().Be("Length");
    }
}
