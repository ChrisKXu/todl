using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis
{
    using ArgumentsList = CommaSeparatedSyntaxList<Argument>;

    public sealed partial class ParserTests
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

        [Theory]
        [InlineData("=", SyntaxKind.EqualsToken)]
        [InlineData("+=", SyntaxKind.PlusEqualsToken)]
        [InlineData("-=", SyntaxKind.MinusEqualsToken)]
        [InlineData("*=", SyntaxKind.StarEqualsToken)]
        [InlineData("/=", SyntaxKind.SlashEqualsToken)]
        public void TestAssignmentOperators(string expectedOperatorToken, SyntaxKind expectedTokenKind)
        {
            var assignmentExpression = ParseExpression<AssignmentExpression>($"a {expectedOperatorToken} (b + 3) * 2");

            assignmentExpression.Left.As<NameExpression>().Text.Should().Be("a");
            assignmentExpression.AssignmentOperator.Text.Should().Be(expectedOperatorToken);
            assignmentExpression.AssignmentOperator.Kind.Should().Be(expectedTokenKind);

            assignmentExpression.Right.As<BinaryExpression>().Invoking(expression =>
            {
                expression.Left.As<ParethesizedExpression>().InnerExpression.As<BinaryExpression>().Invoking(innerExpression =>
                {
                    innerExpression.Left.As<NameExpression>().Text.Should().Be("b");
                    innerExpression.Operator.Text.Should().Be("+");
                    innerExpression.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
                    innerExpression.Right.As<LiteralExpression>().LiteralToken.Text.Should().Be("3");
                });

                expression.Operator.Text.Should().Be("*");
                expression.Operator.Kind.Should().Be(SyntaxKind.StarToken);
                expression.Right.As<LiteralExpression>().Text.Should().Be("2");
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

            blockStatement.OpenBraceToken.Text.Should().Be("{");
            blockStatement.OpenBraceToken.Kind.Should().Be(SyntaxKind.OpenBraceToken);

            blockStatement.InnerStatements.Should().SatisfyRespectively(
                _0 => _0.As<ExpressionStatement>().Should().NotBeNull(),
                _1 => _1.As<ExpressionStatement>().Should().NotBeNull(),
                _2 => _2.As<ExpressionStatement>().Should().NotBeNull());

            blockStatement.CloseBraceToken.Text.Should().Be("}");
            blockStatement.CloseBraceToken.Kind.Should().Be(SyntaxKind.CloseBraceToken);
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

            blockStatement.InnerStatements.Should().SatisfyRespectively(
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
                });
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
