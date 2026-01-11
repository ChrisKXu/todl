using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis
{
    public sealed class ParserTests
    {
        [Theory]
        [InlineData("=", SyntaxKind.EqualsToken)]
        [InlineData("+=", SyntaxKind.PlusEqualsToken)]
        [InlineData("-=", SyntaxKind.MinusEqualsToken)]
        [InlineData("*=", SyntaxKind.StarEqualsToken)]
        [InlineData("/=", SyntaxKind.SlashEqualsToken)]
        public void TestAssignmentOperators(string expectedOperatorToken, SyntaxKind expectedTokenKind)
        {
            var assignmentExpression = TestUtils.ParseExpression<AssignmentExpression>($"a {expectedOperatorToken} (b + 3) * 2");

            assignmentExpression.Left.As<SimpleNameExpression>().Text.Should().Be("a");
            assignmentExpression.AssignmentOperator.Text.Should().Be(expectedOperatorToken);
            assignmentExpression.AssignmentOperator.Kind.Should().Be(expectedTokenKind);

            assignmentExpression.Right.As<BinaryExpression>().Invoking(expression =>
            {
                expression.Left.As<ParethesizedExpression>().InnerExpression.As<BinaryExpression>().Invoking(innerExpression =>
                {
                    innerExpression.Left.As<SimpleNameExpression>().Text.Should().Be("b");
                    innerExpression.Operator.Text.Should().Be("+");
                    innerExpression.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
                    innerExpression.Right.As<LiteralExpression>().LiteralToken.Text.Should().Be("3");
                }).Should().NotThrow();

                expression.Operator.Text.Should().Be("*");
                expression.Operator.Kind.Should().Be(SyntaxKind.StarToken);
                expression.Right.As<LiteralExpression>().Text.Should().Be("2");
            }).Should().NotThrow();
        }

        [Theory]
        [InlineData("System::Threading::Tasks::Task.WhenAny")]
        [InlineData("(1 + 2).ToString")]
        [InlineData("\"abc\".Length")]
        public void TestMemberAccessExpressionBasic(string inputText)
        {
            var memberAccessExpression = TestUtils.ParseExpression<MemberAccessExpression>(inputText);
            memberAccessExpression.Should().NotBeNull();

            var memberName = inputText[(inputText.LastIndexOf('.') + 1)..^0];
            memberAccessExpression.MemberIdentifierToken.Text.Should().Be(memberName);
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
            var blockStatement = TestUtils.ParseStatement<BlockStatement>(inputText);

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
                    letStatement.InitializerExpression.As<SimpleNameExpression>().Text.Should().Be("a");
                });
        }

        [Theory]
        [InlineData("import * from System;")]
        [InlineData("import * from System::Threading::Tasks;")]
        [InlineData("import { Task } from System::Threading::Tasks;")]
        [InlineData("import { ConcurrentBag, ConcurrentDictionary, ConcurrentQueue } from System::Collections::Concurrent;")]
        public void TestParseImportDirective(string inputText)
        {
            var directive = TestUtils.ParseDirective<ImportDirective>(inputText);

            // calculate namespace by directly parsing the text to see
            // if match with the parser results (convert :: to . for comparison)
            var fromPosition = inputText.IndexOf("from");
            var semicolonPosition = inputText.IndexOf(";");
            var expectedNamespace = inputText[(fromPosition + 5)..semicolonPosition].Replace("::", "."); // convert :: to .

            directive.Should().NotBeNull();
            directive.ImportAll.Should().Be(inputText.Contains('*'));
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
    }
}
