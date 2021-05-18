using FluentAssertions;
using Todl.CodeAnalysis;
using Xunit;

namespace Todl.Tests.CodeAnalysis
{
    public class ParserTests
    {
        [Fact]
        public void TestParseBinaryExpressionBasic()
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString("1 + 2 + 3"));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            var binaryExpression = parser.ParseBinaryExpression() as BinaryExpression;
            binaryExpression.Should().NotBeNull();
            binaryExpression.Operator.Text.Should().Be("+");
            (binaryExpression.Right as LiteralExpression).Text.Should().Be("3");

            var left = binaryExpression.Left as BinaryExpression;
            left.Should().NotBeNull();
            left.Operator.Text.Should().Be("+");
            (left.Left as LiteralExpression).Text.Should().Be("1");
            (left.Right as LiteralExpression).Text.Should().Be("2");
        }

        [Fact]
        public void TestParseBinaryExpressionWithPrecedence()
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString("1 + 2 * 3 - 4"));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            var binaryExpression = parser.ParseBinaryExpression() as BinaryExpression;
            binaryExpression.Should().NotBeNull();
            binaryExpression.Operator.Text.Should().Be("-");
            (binaryExpression.Right as LiteralExpression).Text.Should().Be("4");

            var left = binaryExpression.Left as BinaryExpression;
            left.Should().NotBeNull();
            left.Operator.Text.Should().Be("+");
            (left.Left as LiteralExpression).Text.Should().Be("1");

            var multiplicationExpression = left.Right as BinaryExpression;
            multiplicationExpression.Should().NotBeNull();
            multiplicationExpression.Operator.Text.Should().Be("*");
            (multiplicationExpression.Left as LiteralExpression).Text.Should().Be("2");
            (multiplicationExpression.Right as LiteralExpression).Text.Should().Be("3");
        }
    }
}