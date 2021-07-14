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

        private static TStatement ParseStatement<TStatement>(string sourceText)
            where TStatement : Statement
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString(sourceText));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            return parser.ParseStatement() as TStatement;
        }

        [Fact]
        public void TestParseBinaryExpressionBasic()
        {
            var binaryExpression = ParseExpression<BinaryExpression>("1 + 2 + 3");

            binaryExpression.Should().HaveChildren(
                left =>
                {
                    left.Should().HaveChildren(
                        innerLeft => innerLeft.As<LiteralExpression>().Text.Should().Be("1"),
                        operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("+"),
                        right => right.As<LiteralExpression>().Text.Should().Be("2"));
                },
                operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("+"),
                right => right.As<LiteralExpression>().Text.Should().Be("3"));
        }

        [Fact]
        public void TestParseBinaryExpressionWithPrecedence()
        {
            var binaryExpression = ParseExpression<BinaryExpression>("1 + 2 * 3 - 4");

            binaryExpression.Should().HaveChildren(
                left =>
                {
                    left.Should().HaveChildren(
                        left => left.As<LiteralExpression>().Text.Should().Be("1"),
                        operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("+"),
                        multiplication =>
                        {
                            multiplication.Should().HaveChildren(
                                left => left.As<LiteralExpression>().Text.Should().Be("2"),
                                operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("*"),
                                right => right.As<LiteralExpression>().Text.Should().Be("3"));
                        });
                },
                operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("-"),
                right => right.As<LiteralExpression>().Text.Should().Be("4"));
        }

        [Fact]
        public void TestParseBinaryExpressionWithParenthesis()
        {
            var binaryExpression = ParseExpression<BinaryExpression>("(1 + 2) * 3 - 4");

            binaryExpression.Should().HaveChildren(
                left =>
                {
                    left.Should().HaveChildren(
                        left =>
                        {
                            left.Should().HaveChildren(
                                openParenthesisToken => openParenthesisToken.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.OpenParenthesisToken),
                                binaryExpression =>
                                {
                                    binaryExpression.Should().HaveChildren(
                                        left => left.As<LiteralExpression>().Text.Should().Be("1"),
                                        operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("+"),
                                        right => right.As<LiteralExpression>().Text.Should().Be("2"));
                                },
                                closeParenthesisToken => closeParenthesisToken.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.CloseParenthesisToken));
                        },
                        operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("*"),
                        right => right.As<LiteralExpression>().Text.Should().Be("3"));
                },
                operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("-"),
                right => right.As<LiteralExpression>().Text.Should().Be("4"));
        }

        [Fact]
        public void TestParseBinaryExpressionWithEquality()
        {
            var binaryExpression = ParseExpression<BinaryExpression>("3 == 1 + 2");

            binaryExpression.Should().HaveChildren(
                left => left.As<LiteralExpression>().Text.Should().Be("3"),
                operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("=="),
                right =>
                {
                    right.Should().HaveChildren(
                        left => left.As<LiteralExpression>().Text.Should().Be("1"),
                        operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("+"),
                        right => right.As<LiteralExpression>().Text.Should().Be("2"));
                });
        }

        [Fact]
        public void TestParseBinaryExpressionWithInequality()
        {
            var binaryExpression = ParseExpression<BinaryExpression>("5 != 1 + 2");

            binaryExpression.Should().HaveChildren(
                left => left.As<LiteralExpression>().Text.Should().Be("5"),
                operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("!="),
                right =>
                {
                    right.Should().HaveChildren(
                        left => left.As<LiteralExpression>().Text.Should().Be("1"),
                        operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("+"),
                        right => right.As<LiteralExpression>().Text.Should().Be("2"));
                });
        }

        [Fact]
        public void TestParseBinaryExpressionWithNameAndUnaryExpression()
        {
            var binaryExpression = ParseExpression<BinaryExpression>("(++a + 2) * 3 + 4");

            binaryExpression.Should().HaveChildren(
                left =>
                {
                    left.Should().HaveChildren(
                        left =>
                        {
                            left.As<ParethesizedExpression>().InnerExpression.Should().HaveChildren(
                                left =>
                                {
                                    var unaryExpression = left.As<UnaryExpression>();
                                    unaryExpression.Operator.Text.Should().Be("++");
                                    unaryExpression.Operand.As<NameExpression>().IdentifierToken.Text.Should().Be("a");
                                    unaryExpression.Trailing.Should().Be(false);
                                },
                                operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("+"),
                                right => right.As<LiteralExpression>().Text.Should().Be("2"));
                        },
                        operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("*"),
                        right => right.As<LiteralExpression>().Text.Should().Be("3"));
                },
                operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("+"),
                right => right.As<LiteralExpression>().Text.Should().Be("4"));
        }

        [Fact]
        public void TestParseBinaryExpressionWithNameAndTrailingUnaryExpression()
        {
            var binaryExpression = ParseExpression<BinaryExpression>("(a++ + 2) * 3");

            binaryExpression.Should().HaveChildren(
                left =>
                {
                    left.As<ParethesizedExpression>().InnerExpression.Should().HaveChildren(
                        left =>
                        {
                            var unaryExpression = left.As<UnaryExpression>();
                            unaryExpression.Operand.As<NameExpression>().IdentifierToken.Text.Should().Be("a");
                            unaryExpression.Operator.Text.Should().Be("++");
                            unaryExpression.Trailing.Should().Be(true);

                        },
                        operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("+"),
                        right => right.As<LiteralExpression>().Text.Should().Be("2"));
                },
                operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("*"),
                right => right.As<LiteralExpression>().Text.Should().Be("3"));
        }

        [Theory]
        [InlineData("=", SyntaxKind.EqualsToken)]
        [InlineData("+=", SyntaxKind.PlusEqualsToken)]
        [InlineData("-=", SyntaxKind.MinusEqualsToken)]
        [InlineData("*=", SyntaxKind.StarEqualsToken)]
        [InlineData("/=", SyntaxKind.SlashEqualsToken)]
        public void TestAssignmentOperators(string expectedOperatorToken, SyntaxKind expectedTokenKind)
        {
            var assignmentExpression = ParseExpression<AssignmentExpression>($"a {expectedOperatorToken} (b + 3) * 2");

            assignmentExpression.Should().HaveChildren(
                identifierToken => identifierToken.As<SyntaxToken>().Text.Should().Be("a"),
                operatorToken =>
                {
                    var assignmentOperator = operatorToken.As<SyntaxToken>();
                    assignmentOperator.Text.Should().Be(expectedOperatorToken);
                    assignmentOperator.Kind.Should().Be(expectedTokenKind);
                },
                expression =>
                {
                    expression.Should().HaveChildren(
                        left =>
                        {
                            var innerExpression = left.As<ParethesizedExpression>().InnerExpression.As<BinaryExpression>();
                            innerExpression.Left.As<NameExpression>().IdentifierToken.Text.Should().Be("b");
                            innerExpression.Operator.Text.Should().Be("+");
                            innerExpression.Right.As<LiteralExpression>().LiteralToken.Text.Should().Be("3");
                        },
                        operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("*"),
                        right => right.As<LiteralExpression>().Text.Should().Be("2"));
                });
        }

        [Fact]
        public void TestParseBlockStatementBasic()
        {
            var inputText = @"
            {
                a = 1 + 2;
                ++a;
                b = a;
            }
            ";
            var blockStatement = ParseStatement<BlockStatement>(inputText);

            blockStatement.Should().HaveChildren(
                openBrace => openBrace.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.OpenBraceToken),
                _0 => _0.As<ExpressionStatement>().Should().NotBeNull(),
                _1 => _1.As<ExpressionStatement>().Should().NotBeNull(),
                _2 => _2.As<ExpressionStatement>().Should().NotBeNull(),
                closeBrace => closeBrace.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.CloseBraceToken));
        }

        [Fact]
        public void TestParseVariableDeclarationStatementBasic()
        {
            var inputText = @"
            {
                const a = 0;
                let b = a;
            }
            ";
            var blockStatement = ParseStatement<BlockStatement>(inputText);

            blockStatement.Should().HaveChildren(
                openBrace => openBrace.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.OpenBraceToken),
                _0 =>
                {
                    var constStatement = _0.As<VariableDeclarationStatement>();
                    constStatement.IdentifierToken.Text.Should().Be("a");
                    constStatement.InitializerExpression.As<LiteralExpression>().LiteralToken.Text.Should().Be("0");
                },
                _1 =>
                {
                    var letStatement = _1.As<VariableDeclarationStatement>();
                    letStatement.IdentifierToken.Text.Should().Be("b");
                    letStatement.InitializerExpression.As<NameExpression>().IdentifierToken.Text.Should().Be("a");
                },
                closeBrace => closeBrace.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.CloseBraceToken));
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
