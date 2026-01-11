using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;
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

        [Theory]
        [InlineData("a.b.c", 2)]
        [InlineData("a.b.c.d", 3)]
        [InlineData("a.b.c.d.e", 4)]
        public void TestChainedMemberAccess(string inputText, int expectedChainLength)
        {
            var expression = TestUtils.ParseExpression<MemberAccessExpression>(inputText);
            expression.Should().NotBeNull();

            // Count the chain depth
            var depth = 1;
            var current = expression.BaseExpression;
            while (current is MemberAccessExpression inner)
            {
                depth++;
                current = inner.BaseExpression;
            }

            depth.Should().Be(expectedChainLength);
            current.Should().BeOfType<SimpleNameExpression>();
        }

        [Fact]
        public void TestChainedMemberAccessWithQualifiedName()
        {
            var expression = TestUtils.ParseExpression<MemberAccessExpression>("System::Console.Out.WriteLine");

            expression.Should().NotBeNull();
            expression.MemberIdentifierToken.Text.Should().Be("WriteLine");

            var innerAccess = expression.BaseExpression.As<MemberAccessExpression>();
            innerAccess.MemberIdentifierToken.Text.Should().Be("Out");

            innerAccess.BaseExpression.Should().BeOfType<NamespaceQualifiedNameExpression>();
        }

        [Fact]
        public void TestChainedAssignment()
        {
            // a = b = c = 5 should be right-associative: a = (b = (c = 5))
            var expression = TestUtils.ParseExpression<AssignmentExpression>("a = b = c = 5");

            expression.Should().NotBeNull();
            expression.Left.As<SimpleNameExpression>().Text.Should().Be("a");
            expression.AssignmentOperator.Kind.Should().Be(SyntaxKind.EqualsToken);

            var inner1 = expression.Right.As<AssignmentExpression>();
            inner1.Left.As<SimpleNameExpression>().Text.Should().Be("b");

            var inner2 = inner1.Right.As<AssignmentExpression>();
            inner2.Left.As<SimpleNameExpression>().Text.Should().Be("c");
            inner2.Right.As<LiteralExpression>().Text.Should().Be("5");
        }

        [Fact]
        public void TestMemberAccessOnLiteralString()
        {
            var expression = TestUtils.ParseExpression<MemberAccessExpression>("\"hello\".Length");

            expression.Should().NotBeNull();
            expression.MemberIdentifierToken.Text.Should().Be("Length");
            expression.BaseExpression.Should().BeOfType<LiteralExpression>();
        }

        [Fact]
        public void TestMemberAccessOnParenthesizedExpression()
        {
            var expression = TestUtils.ParseExpression<MemberAccessExpression>("(a + b).ToString");

            expression.Should().NotBeNull();
            expression.MemberIdentifierToken.Text.Should().Be("ToString");
            expression.BaseExpression.Should().BeOfType<ParethesizedExpression>();
        }

        [Fact]
        public void TestMemberAccessOnNewExpression()
        {
            var expression = TestUtils.ParseExpression<MemberAccessExpression>("new System::Object().ToString");

            expression.Should().NotBeNull();
            expression.MemberIdentifierToken.Text.Should().Be("ToString");
            expression.BaseExpression.Should().BeOfType<NewExpression>();
        }

        #region Error Recovery Tests

        [Fact]
        public void TestVariableDeclarationMissingSemicolon()
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var statement = TestUtils.ParseStatement<VariableDeclarationStatement>(
                "const a = 5", diagnosticBuilder);

            statement.Should().NotBeNull();
            statement.SemicolonToken.Missing.Should().BeTrue();
        }

        [Fact]
        public void TestExpressionStatementMissingSemicolon()
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var statement = TestUtils.ParseStatement<ExpressionStatement>(
                "a = 5", diagnosticBuilder);

            statement.Should().NotBeNull();
            statement.SemicolonToken.Missing.Should().BeTrue();
        }

        [Fact]
        public void TestReturnStatementMissingSemicolon()
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var statement = TestUtils.ParseStatement<ReturnStatement>(
                "return 5", diagnosticBuilder);

            statement.Should().NotBeNull();
            statement.SemicolonToken.Missing.Should().BeTrue();
        }

        [Fact]
        public void TestBreakStatementMissingSemicolon()
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var statement = TestUtils.ParseStatement<BreakStatement>(
                "break", diagnosticBuilder);

            statement.Should().NotBeNull();
            statement.SemicolonToken.Missing.Should().BeTrue();
        }

        [Fact]
        public void TestContinueStatementMissingSemicolon()
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var statement = TestUtils.ParseStatement<ContinueStatement>(
                "continue", diagnosticBuilder);

            statement.Should().NotBeNull();
            statement.SemicolonToken.Missing.Should().BeTrue();
        }

        [Fact]
        public void TestParenthesizedExpressionMissingCloseParen()
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var expression = TestUtils.ParseExpression<ParethesizedExpression>(
                "(a + b", diagnosticBuilder);

            expression.Should().NotBeNull();
            expression.RightParenthesisToken.Missing.Should().BeTrue();
        }

        [Fact]
        public void TestNewExpressionMissingCloseParen()
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var expression = TestUtils.ParseExpression<NewExpression>(
                "new System::Object(", diagnosticBuilder);

            expression.Should().NotBeNull();
            expression.Arguments.CloseParenthesisToken.Missing.Should().BeTrue();
        }

        [Fact]
        public void TestImportDirectiveMissingSemicolon()
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var directive = TestUtils.ParseDirective<ImportDirective>(
                "import * from System", diagnosticBuilder);

            directive.Should().NotBeNull();
            directive.SemicolonToken.Missing.Should().BeTrue();
        }

        [Fact]
        public void TestFunctionDeclarationMissingBody()
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var member = TestUtils.ParseMember<FunctionDeclarationMember>(
                "void Function()", diagnosticBuilder);

            member.Should().NotBeNull();
            member.Body.OpenBraceToken.Missing.Should().BeTrue();
        }

        [Fact]
        public void TestIfStatementMissingBlock()
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var statement = TestUtils.ParseStatement<IfUnlessStatement>(
                "if a == 0", diagnosticBuilder);

            statement.Should().NotBeNull();
            statement.BlockStatement.OpenBraceToken.Missing.Should().BeTrue();
        }

        [Fact]
        public void TestWhileStatementMissingBlock()
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var statement = TestUtils.ParseStatement<WhileUntilStatement>(
                "while a == 0", diagnosticBuilder);

            statement.Should().NotBeNull();
            statement.BlockStatement.OpenBraceToken.Missing.Should().BeTrue();
        }

        [Fact]
        public void TestVariableDeclarationMissingIdentifier()
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var statement = TestUtils.ParseStatement<VariableDeclarationStatement>(
                "const = 5;", diagnosticBuilder);

            statement.Should().NotBeNull();
            statement.IdentifierToken.Missing.Should().BeTrue();
        }

        [Fact]
        public void TestVariableDeclarationMissingEquals()
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var statement = TestUtils.ParseStatement<VariableDeclarationStatement>(
                "const a 5;", diagnosticBuilder);

            statement.Should().NotBeNull();
            statement.AssignmentToken.Missing.Should().BeTrue();
        }

        [Fact]
        public void TestImportDirectiveWithEmptyBraces()
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var directive = TestUtils.ParseDirective<ImportDirective>(
                "import { } from System;", diagnosticBuilder);

            directive.Should().NotBeNull();
            directive.ImportedNames.Should().BeEmpty();
        }

        [Fact]
        public void TestBlockStatementMissingOpenBrace()
        {
            var diagnosticBuilder = new DiagnosticBag.Builder();
            var member = TestUtils.ParseMember<FunctionDeclarationMember>(
                "void Func() a = 1; }", diagnosticBuilder);

            member.Should().NotBeNull();
            member.Body.OpenBraceToken.Missing.Should().BeTrue();
        }

        #endregion

        #region Parenthesized Expression Tests

        [Fact]
        public void TestDeeplyNestedParentheses()
        {
            var expression = TestUtils.ParseExpression<ParethesizedExpression>("(((a)))");

            expression.Should().NotBeNull();
            var level1 = expression.InnerExpression.As<ParethesizedExpression>();
            level1.Should().NotBeNull();
            var level2 = level1.InnerExpression.As<ParethesizedExpression>();
            level2.Should().NotBeNull();
            level2.InnerExpression.As<SimpleNameExpression>().Text.Should().Be("a");
        }

        [Fact]
        public void TestParenthesizedBinaryExpression()
        {
            var expression = TestUtils.ParseExpression<ParethesizedExpression>("(a + b * c)");

            expression.Should().NotBeNull();
            expression.InnerExpression.Should().BeOfType<BinaryExpression>();
        }

        #endregion

        #region Type Expression Tests

        [Theory]
        [InlineData("int[]")]
        [InlineData("string[]")]
        [InlineData("bool[]")]
        public void TestParseArrayTypeInFunctionReturn(string arrayType)
        {
            var member = TestUtils.ParseMember<FunctionDeclarationMember>($"{arrayType} Func() {{}}");

            member.Should().NotBeNull();
            member.ReturnType.IsArrayType.Should().BeTrue();
            member.ReturnType.ArrayRankSpecifiers.Should().HaveCount(1);
        }

        [Fact]
        public void TestParseMultiDimensionalArrayType()
        {
            var member = TestUtils.ParseMember<FunctionDeclarationMember>("int[][] Func() {}");

            member.Should().NotBeNull();
            member.ReturnType.IsArrayType.Should().BeTrue();
            member.ReturnType.ArrayRankSpecifiers.Should().HaveCount(2);
        }

        [Fact]
        public void TestParseQualifiedArrayType()
        {
            var member = TestUtils.ParseMember<FunctionDeclarationMember>("System::Uri[] Func() {}");

            member.Should().NotBeNull();
            member.ReturnType.IsArrayType.Should().BeTrue();
            member.ReturnType.BaseTypeExpression.Should().BeOfType<NamespaceQualifiedNameExpression>();
        }

        #endregion

        #region Variable Declaration Member Tests

        [Theory]
        [InlineData("const a = 5;", "a", "5")]
        [InlineData("let b = 10;", "b", "10")]
        [InlineData("const message = \"hello\";", "message", "\"hello\"")]
        public void TestParseVariableDeclarationMember(string input, string expectedName, string expectedValue)
        {
            var member = TestUtils.ParseMember<VariableDeclarationMember>(input);

            member.Should().NotBeNull();
            member.VariableDeclarationStatement.IdentifierToken.Text.Should().Be(expectedName);
            member.VariableDeclarationStatement.InitializerExpression.As<LiteralExpression>().Text.Should().Be(expectedValue);
        }

        [Fact]
        public void TestParseVariableDeclarationMemberWithExpression()
        {
            var member = TestUtils.ParseMember<VariableDeclarationMember>("const sum = 1 + 2 + 3;");

            member.Should().NotBeNull();
            member.VariableDeclarationStatement.IdentifierToken.Text.Should().Be("sum");
            member.VariableDeclarationStatement.InitializerExpression.Should().BeOfType<BinaryExpression>();
        }

        #endregion
    }
}
