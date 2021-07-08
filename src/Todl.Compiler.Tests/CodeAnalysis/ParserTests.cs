using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis
{
    public class ParserTests
    {
        private static TExpression ParseExpression<TExpression>(string sourceText)
            where TExpression : Expression
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString(sourceText));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            return parser.ParseExpression() as TExpression;
        }

        [Fact]
        public void TestParseBinaryExpressionBasic()
        {
            var binaryExpression = ParseExpression<BinaryExpression>("1 + 2 + 3");
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
            var binaryExpression = ParseExpression<BinaryExpression>("1 + 2 * 3 - 4");
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
            var binaryExpression = ParseExpression<BinaryExpression>("(1 + 2) * 3 - 4");
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
            var binaryExpression = ParseExpression<BinaryExpression>("3 == 1 + 2");
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
            var binaryExpression = ParseExpression<BinaryExpression>("5 != 1 + 2");
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
        public void TestParseBinaryExpressionWithNameAndUnaryExpression()
        {
            var binaryExpression = ParseExpression<BinaryExpression>("(++a + 2) * 3 + 4");
            binaryExpression.Should().NotBeNull();
            binaryExpression.Operator.Text.Should().Be("+");
            (binaryExpression.Right as LiteralExpression).Text.Should().Be("4");

            var left = binaryExpression.Left as BinaryExpression;
            left.Should().NotBeNull();
            left.Operator.Text.Should().Be("*");
            (left.Right as LiteralExpression).Text.Should().Be("3");

            var parethesizedExpression = left.Left as ParethesizedExpression;
            var innerExpression = parethesizedExpression.InnerExpression as BinaryExpression;
            innerExpression.Should().NotBeNull();
            innerExpression.Operator.Text.Should().Be("+");
            (innerExpression.Right as LiteralExpression).Text.Should().Be("2");

            var unaryExpression = innerExpression.Left as UnaryExpression;
            unaryExpression.Should().NotBeNull();
            (unaryExpression.Operand as NameExpression).IdentifierToken.Text.Should().Be("a");
            unaryExpression.Operator.Text.Should().Be("++");
            unaryExpression.Trailing.Should().Be(false);
        }

        [Fact]
        public void TestParseBinaryExpressionWithNameAndTrailingUnaryExpression()
        {
            var binaryExpression = ParseExpression<BinaryExpression>("(a++ + 2) * 3");
            binaryExpression.Should().NotBeNull();
            binaryExpression.Operator.Text.Should().Be("*");
            (binaryExpression.Right as LiteralExpression).Text.Should().Be("3");

            var innerExpression = (binaryExpression.Left as ParethesizedExpression).InnerExpression as BinaryExpression;
            innerExpression.Should().NotBeNull();
            innerExpression.Operator.Text.Should().Be("+");
            (innerExpression.Right as LiteralExpression).Text.Should().Be("2");

            var unaryExpression = innerExpression.Left as UnaryExpression;
            unaryExpression.Operator.Text.Should().Be("++");
            (unaryExpression.Operand as NameExpression).IdentifierToken.Text.Should().Be("a");
            unaryExpression.Trailing.Should().Be(true);
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
