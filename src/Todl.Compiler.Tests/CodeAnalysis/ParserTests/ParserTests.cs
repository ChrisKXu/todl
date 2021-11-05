using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis
{
    using ArgumentsList = CommaSeparatedSyntaxList<Argument>;

    public partial class ParserTests
    {
        private static TExpression ParseExpression<TExpression>(string sourceText)
            where TExpression : Expression
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString(sourceText));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            return parser.ParseExpression().As<TExpression>();
        }

        private static TStatement ParseStatement<TStatement>(string sourceText)
            where TStatement : Statement
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString(sourceText));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            return parser.ParseStatement().As<TStatement>();
        }

        private static TDirective ParseDirective<TDirective>(string sourceText)
            where TDirective : Directive
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString(sourceText));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            return parser.ParseDirective().As<TDirective>();
        }

        private static TMember ParseMember<TMember>(string sourceText)
            where TMember : Member
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString(sourceText));
            var parser = new Parser(syntaxTree);
            parser.Lex();

            return parser.ParseMember().As<TMember>();
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
                                    unaryExpression.Operand.As<NameExpression>().QualifiedName.Should().Be("a");
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
                            unaryExpression.Operand.As<NameExpression>().QualifiedName.Should().Be("a");
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
                left => left.As<NameExpression>().QualifiedName.Should().Be("a"),
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
                            innerExpression.Left.As<NameExpression>().QualifiedName.Should().Be("b");
                            innerExpression.Operator.Text.Should().Be("+");
                            innerExpression.Right.As<LiteralExpression>().LiteralToken.Text.Should().Be("3");
                        },
                        operatorToken => operatorToken.As<SyntaxToken>().Text.Should().Be("*"),
                        right => right.As<LiteralExpression>().Text.Should().Be("2"));
                });
        }

        [Theory]
        [InlineData("System.Threading.Tasks.Task.WhenAny")]
        [InlineData("(1 + 2).ToString")]
        [InlineData("\"abc\".Length")]
        public void TestMemberAccessExpressionBasic(string inputText)
        {
            var memberAccessExpression = ParseExpression<MemberAccessExpression>(inputText);
            memberAccessExpression.Should().NotBeNull();

            var memberName = inputText[(inputText.LastIndexOf('.') + 1)..^0];
            memberAccessExpression.MemberIdentifierToken.Text.Should().Be(memberName);
        }

        [Fact]
        public void TestParseFunctionCallExpressionWithoutArguments()
        {
            var inputText = "a.ToString()";
            var functionCallExpression = ParseExpression<FunctionCallExpression>(inputText);

            functionCallExpression.Should().HaveChildren(
                _0 => _0.As<NameExpression>().QualifiedName.Should().Be("a"),
                _1 => _1.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.DotToken),
                _2 => _2.As<SyntaxToken>().Text.Should().Be("ToString"),
                arguments => arguments.As<ArgumentsList>().Should().HaveChildren(
                    openParenthesisToken => openParenthesisToken.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.OpenParenthesisToken),
                    closeParenthesisToken => closeParenthesisToken.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.CloseParenthesisToken)));
        }

        [Fact]
        public void TestParseFunctionCallExpressionWithOnePositionalArgument()
        {
            var inputText = "System.Int32.Parse(\"123\")";
            var functionCallExpression = ParseExpression<FunctionCallExpression>(inputText);
            functionCallExpression.Should().NotBeNull();

            functionCallExpression.Should().HaveChildren(
                _0 => _0.As<NameExpression>().QualifiedName.Should().Be("System.Int32"),
                _1 => _1.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.DotToken),
                _2 => _2.As<SyntaxToken>().Text.Should().Be("Parse"),
                arguments => arguments.As<ArgumentsList>().Should().HaveChildren(
                    openParenthesisToken => openParenthesisToken.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.OpenParenthesisToken),
                    _0 =>
                    {
                        var argument = _0.As<Argument>();
                        argument.IsNamedArgument.Should().Be(false);
                        argument.Expression.As<LiteralExpression>().Text.Should().Be("\"123\"");
                    },
                    closeParenthesisToken => closeParenthesisToken.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.CloseParenthesisToken)));
        }

        [Fact]
        public void TestParseFunctionCallExpressionWithOneNamedArgument()
        {
            var inputText = "System.Int32.Parse(s: \"123\")";
            var functionCallExpression = ParseExpression<FunctionCallExpression>(inputText);
            functionCallExpression.Should().NotBeNull();

            functionCallExpression.Should().HaveChildren(
                _0 => _0.As<NameExpression>().QualifiedName.Should().Be("System.Int32"),
                _1 => _1.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.DotToken),
                _2 => _2.As<SyntaxToken>().Text.Should().Be("Parse"),
                arguments => arguments.As<ArgumentsList>().Should().HaveChildren(
                    openParenthesisToken => openParenthesisToken.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.OpenParenthesisToken),
                    _0 =>
                    {
                        var argument = _0.As<Argument>();
                        argument.IsNamedArgument.Should().Be(true);
                        argument.Identifier.Text.Should().Be("s");
                        argument.ColonToken.Kind.Should().Be(SyntaxKind.ColonToken);
                        argument.Expression.As<LiteralExpression>().Text.Should().Be("\"123\"");
                    },
                    closeParenthesisToken => closeParenthesisToken.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.CloseParenthesisToken)));
        }

        [Fact]
        public void TestParseNewExpressionBasicWithNoArguments()
        {
            var inputText = "new System.Exception()";
            var newExpression = ParseExpression<NewExpression>(inputText);

            newExpression.Should().HaveChildren(
                _0 => _0.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.NewKeywordToken),
                _1 => _1.As<NameExpression>().QualifiedName.Should().Be("System.Exception"),
                _2 => _2.As<ArgumentsList>().Items.Should().BeEmpty());
        }

        [Fact]
        public void TestParseNewExpressionBasicWithOnePositionalArgument()
        {
            var inputText = "new System.Uri(\"https://google.com\")";
            var newExpression = ParseExpression<NewExpression>(inputText);

            newExpression.Should().HaveChildren(
                _0 => _0.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.NewKeywordToken),
                _1 => _1.As<NameExpression>().QualifiedName.Should().Be("System.Uri"),
                _2 =>
                {
                    var arguments = _2.As<ArgumentsList>().Items;
                    arguments.Should().HaveCount(1);
                    arguments[0].IsNamedArgument.Should().BeFalse();
                    arguments[0].Expression.As<LiteralExpression>().LiteralToken.Text.Should().Be("\"https://google.com\"");
                });
        }

        [Fact]
        public void TestParseNewExpressionBasicWithOneNamedArgument()
        {
            var inputText = "new System.Uri(uriString: \"https://google.com\")";
            var newExpression = ParseExpression<NewExpression>(inputText);

            newExpression.Should().HaveChildren(
                _0 => _0.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.NewKeywordToken),
                _1 => _1.As<NameExpression>().QualifiedName.Should().Be("System.Uri"),
                _2 =>
                {
                    var arguments = _2.As<ArgumentsList>().Items;
                    arguments.Should().HaveCount(1);

                    var uriString = arguments[0];
                    uriString.IsNamedArgument.Should().BeTrue();
                    uriString.Identifier.Text.Should().Be("uriString");
                    uriString.Expression.As<LiteralExpression>().LiteralToken.Text.Should().Be("\"https://google.com\"");
                });
        }

        [Fact]
        public void TestParseNewExpressionBasicWithMultiplePositionalArguments()
        {
            var inputText = "new System.Uri(\"https://google.com\", false)";
            var newExpression = ParseExpression<NewExpression>(inputText);

            newExpression.Should().HaveChildren(
                _0 => _0.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.NewKeywordToken),
                _1 => _1.As<NameExpression>().QualifiedName.Should().Be("System.Uri"),
                _2 =>
                {
                    var arguments = _2.As<ArgumentsList>().Items;
                    arguments.Should().HaveCount(2);
                    arguments[0].IsNamedArgument.Should().BeFalse();
                    arguments[0].Expression.As<LiteralExpression>().LiteralToken.Text.Should().Be("\"https://google.com\"");
                    arguments[1].IsNamedArgument.Should().BeFalse();
                    arguments[1].Expression.As<LiteralExpression>().LiteralToken.Text.Should().Be("false");
                });
        }

        [Fact]
        public void TestParseNewExpressionBasicWithMultipleNamedArguments()
        {
            var inputText = "new System.Uri(uriString: \"https://google.com\", dontEscape: false)";
            var newExpression = ParseExpression<NewExpression>(inputText);

            newExpression.Should().HaveChildren(
                _0 => _0.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.NewKeywordToken),
                _1 => _1.As<NameExpression>().QualifiedName.Should().Be("System.Uri"),
                _2 =>
                {
                    var arguments = _2.As<ArgumentsList>().Items;
                    arguments.Should().HaveCount(2);

                    var uriString = arguments[0];
                    uriString.IsNamedArgument.Should().BeTrue();
                    uriString.Identifier.Text.Should().Be("uriString");
                    uriString.Expression.As<LiteralExpression>().LiteralToken.Text.Should().Be("\"https://google.com\"");

                    var dontEscape = arguments[1];
                    dontEscape.IsNamedArgument.Should().BeTrue();
                    dontEscape.Identifier.Text.Should().Be("dontEscape");
                    dontEscape.Expression.As<LiteralExpression>().LiteralToken.Text.Should().Be("false");
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
                    letStatement.InitializerExpression.As<NameExpression>().QualifiedName.Should().Be("a");
                },
                closeBrace => closeBrace.As<SyntaxToken>().Kind.Should().Be(SyntaxKind.CloseBraceToken));
        }

        [Theory]
        [InlineData("import * from System;")]
        [InlineData("import * from System.Threading.Tasks;")]
        [InlineData("import { Task } from System.Threading.Tasks;")]
        [InlineData("import { ConcurrentBag, ConcurrentDictionary, ConcurrentQueue } from System.Collections.Concurrent;")]
        public void TestParseImportDirective(string inputText)
        {
            var directive = ParseDirective<ImportDirective>(inputText);

            // calculate namespace by directly parsing the text to see
            // if match with the parser results
            var fromPosition = inputText.IndexOf("from");
            var semicolonPosition = inputText.IndexOf(";");
            var expectedNamespace = inputText[(fromPosition + 5)..semicolonPosition]; // assuming only one space character after 'from'

            directive.Should().NotBeNull();
            directive.ImportAll.Should().Be(inputText.Contains("*"));
            directive.Namespace.Should().Be(expectedNamespace);

            if (!directive.ImportAll)
            {
                var openBracePosition = inputText.IndexOf("{");
                var closeBracePosition = inputText.IndexOf("}");
                var importedNames =
                    inputText[(openBracePosition + 1)..closeBracePosition]
                    .Split(",")
                    .Select(name => name.Trim());

                directive.ImportedNames.Should().BeEquivalentTo(importedNames);
            }
        }

        [Fact]
        public void TestParserWithDiagnostics()
        {
            var syntaxTree = new SyntaxTree(SourceText.FromString("(1 + 2 * 3 - 4")); // missing a ")"
            var parser = new Parser(syntaxTree);
            parser.Lex();
            parser.ParseExpression();

            parser.Diagnostics.Should().NotBeEmpty();
            parser.Diagnostics[0].TextLocation.TextSpan.Start.Should().Be(14);
            parser.Diagnostics[0].TextLocation.TextSpan.Length.Should().Be(0);
        }
    }
}
