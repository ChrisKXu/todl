using FluentAssertions;
using Todl.Compiler.CodeAnalysis;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis
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

        [Fact]
        public void TestParseBinaryExpressionWithParenthesis()
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString("(1 + 2) * 3 - 4"));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            var binaryExpression = parser.ParseBinaryExpression() as BinaryExpression;
            binaryExpression.Should().NotBeNull();
            binaryExpression.Operator.Text.Should().Be("-");
            (binaryExpression.Right as LiteralExpression).Text.Should().Be("4");

            var left = binaryExpression.Left as BinaryExpression;
            left.Should().NotBeNull();
            left.Operator.Text.Should().Be("*");
            (left.Right as LiteralExpression).Text.Should().Be("3");

            var parenthesizedExpression = left.Left as ParethesizedExpression;
            parenthesizedExpression.Should().NotBeNull();

            var innerExpression = parenthesizedExpression.InnerExpression as BinaryExpression;
            innerExpression.Should().NotBeNull();
            (innerExpression.Left as LiteralExpression).Text.Should().Be("1");
            (innerExpression.Right as LiteralExpression).Text.Should().Be("2");
            innerExpression.Operator.Text.Should().Be("+");
        }

        [Fact]
        public void TestParseBinaryExpressionWithEquality()
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString("3 == 1 + 2"));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            var binaryExpression = parser.ParseBinaryExpression() as BinaryExpression;
            binaryExpression.Should().NotBeNull();
            binaryExpression.Operator.Text.Should().Be("==");
            (binaryExpression.Left as LiteralExpression).Text.Should().Be("3");

            var right = binaryExpression.Right as BinaryExpression;
            right.Should().NotBeNull();
            right.Operator.Text.Should().Be("+");
            (right.Left as LiteralExpression).Text.Should().Be("1");
            (right.Right as LiteralExpression).Text.Should().Be("2");
        }

        [Fact]
        public void TestParseBinaryExpressionWithInequality()
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString("5 != 1 + 2"));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            var binaryExpression = parser.ParseBinaryExpression() as BinaryExpression;
            binaryExpression.Should().NotBeNull();
            binaryExpression.Operator.Text.Should().Be("!=");
            (binaryExpression.Left as LiteralExpression).Text.Should().Be("5");

            var right = binaryExpression.Right as BinaryExpression;
            right.Should().NotBeNull();
            right.Operator.Text.Should().Be("+");
            (right.Left as LiteralExpression).Text.Should().Be("1");
            (right.Right as LiteralExpression).Text.Should().Be("2");
        }

        [Fact]
        public void TestParserWithDiagnostics()
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString("(1 + 2 * 3 - 4")); // missing a ")"
            var parser = new Parser(syntaxTree);
            parser.Parse();

            parser.Diagnostics.Should().NotBeEmpty();
            parser.Diagnostics[0].TextLocation.TextSpan.Start.Should().Be(14);
            parser.Diagnostics[0].TextLocation.TextSpan.Length.Should().Be(0);
        }
    }
}