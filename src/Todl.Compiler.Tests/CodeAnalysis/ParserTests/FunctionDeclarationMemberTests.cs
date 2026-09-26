using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis
{
    public sealed class FunctionDeclarationMemberTests
    {
        [Fact]
        public void ParseFunctionDeclarationMemberWithSimpleReturnType()
        {
            var function = TestUtils.ParseMember<FunctionDeclarationMember>("int Function() {}");
            function.Should().NotBeNull();
            function.Name.Text.ToString().Should().Be("Function");
            function.ReturnType.GetText().Should().Be("int");
            function.Parameters.Items.Should().BeEmpty();
            function.Body.InnerStatements.Should().BeEmpty();
        }

        [Fact]
        public void ParseFunctionDeclarationMemberWithQualifiedReturnType()
        {
            var function = TestUtils.ParseMember<FunctionDeclarationMember>("System::Uri Function() {}");
            function.Should().NotBeNull();
            function.Name.Text.ToString().Should().Be("Function");
            function.ReturnType.GetText().Should().Be("System::Uri");
            function.Parameters.Items.Should().BeEmpty();
            function.Body.InnerStatements.Should().BeEmpty();
        }

        [Fact]
        public void ParseFunctionDeclarationMemberWithArrayReturnType()
        {
            var function = TestUtils.ParseMember<FunctionDeclarationMember>("int[] Function() {}");
            function.Should().NotBeNull();
            function.Name.Text.ToString().Should().Be("Function");
            function.ReturnType.GetText().Should().Be("int[]");
            function.ReturnType.IsArrayType.Should().BeTrue();
            function.Parameters.Items.Should().BeEmpty();
            function.Body.InnerStatements.Should().BeEmpty();
        }

        [Fact]
        public void ParseFunctionDeclarationMemberWithSingleParameter()
        {
            var function = TestUtils.ParseMember<FunctionDeclarationMember>("void Function(int a) {}");
            function.Should().NotBeNull();
            function.Name.Text.ToString().Should().Be("Function");
            function.ReturnType.GetText().Should().Be("void");
            function.Parameters.Items.Should().HaveCount(1);
            function.Body.InnerStatements.Should().BeEmpty();

            var a = function.Parameters.Items[0];
            a.ParameterType.GetText().Should().Be("int");
            a.Identifier.Text.ToString().Should().Be("a");
        }

        [Fact]
        public void ParseFunctionDeclarationMemberWithMultipleParameters()
        {
            var function = TestUtils.ParseMember<FunctionDeclarationMember>("void Function(int a, System::Uri b) {}");
            function.Should().NotBeNull();
            function.Name.Text.ToString().Should().Be("Function");
            function.ReturnType.GetText().Should().Be("void");
            function.Parameters.Items.Should().HaveCount(2);
            function.Body.InnerStatements.Should().BeEmpty();

            var a = function.Parameters.Items[0];
            a.ParameterType.GetText().Should().Be("int");
            a.Identifier.Text.ToString().Should().Be("a");

            var b = function.Parameters.Items[1];
            b.ParameterType.GetText().Should().Be("System::Uri");
            b.Identifier.Text.ToString().Should().Be("b");
        }

        [Fact]
        public void ParseFunctionDeclarationMemberWithArrayParameters()
        {
            var function = TestUtils.ParseMember<FunctionDeclarationMember>("void Function(string[] a, System::Uri[] b) {}");
            function.Should().NotBeNull();
            function.Name.Text.ToString().Should().Be("Function");
            function.ReturnType.GetText().Should().Be("void");
            function.Parameters.Items.Should().HaveCount(2);
            function.Body.InnerStatements.Should().BeEmpty();

            var a = function.Parameters.Items[0];
            a.ParameterType.GetText().Should().Be("string[]");
            a.Identifier.Text.ToString().Should().Be("a");
            a.ParameterType.IsArrayType.Should().BeTrue();

            var b = function.Parameters.Items[1];
            b.ParameterType.GetText().Should().Be("System::Uri[]");
            b.Identifier.Text.ToString().Should().Be("b");
            b.ParameterType.IsArrayType.Should().BeTrue();
        }

        [Fact]
        public void ParseFunctionDeclarationMemberWithMultiDimensionalArrayParameters()
        {
            var function = TestUtils.ParseMember<FunctionDeclarationMember>("void Function(int intParameter, string[] stringArrayParameter, System::Uri[][] twoDimensionalArraysParameter) {}");
            function.Should().NotBeNull();
            function.Name.Text.ToString().Should().Be("Function");
            function.ReturnType.GetText().Should().Be("void");
            function.Parameters.Items.Should().HaveCount(3);
            function.Body.InnerStatements.Should().BeEmpty();

            var intParameter = function.Parameters.Items[0];
            intParameter.ParameterType.GetText().Should().Be("int");
            intParameter.Identifier.Text.ToString().Should().Be(nameof(intParameter));
            intParameter.ParameterType.IsArrayType.Should().BeFalse();

            var stringArrayParameter = function.Parameters.Items[1];
            stringArrayParameter.ParameterType.GetText().Should().Be("string[]");
            stringArrayParameter.Identifier.Text.ToString().Should().Be(nameof(stringArrayParameter));
            stringArrayParameter.ParameterType.IsArrayType.Should().BeTrue();

            var twoDimensionalArraysParameter = function.Parameters.Items[2];
            twoDimensionalArraysParameter.ParameterType.GetText().Should().Be("System::Uri[][]");
            twoDimensionalArraysParameter.Identifier.Text.ToString().Should().Be(nameof(twoDimensionalArraysParameter));
            twoDimensionalArraysParameter.ParameterType.IsArrayType.Should().BeTrue();
        }

        [Fact]
        public void ParseEmptyReturnStatement()
        {
            var emptyReturnStatement = TestUtils.ParseStatement<ReturnStatement>("return;");
            emptyReturnStatement.Should().NotBeNull();
            emptyReturnStatement.ReturnKeywordToken.Text.ToString().Should().Be("return");
            emptyReturnStatement.ReturnKeywordToken.Kind.Should().Be(SyntaxKind.ReturnKeywordToken);
            emptyReturnStatement.ReturnValueExpression.Should().BeNull();
            emptyReturnStatement.SemicolonToken.Text.ToString().Should().Be(";");
            emptyReturnStatement.SemicolonToken.Kind.Should().Be(SyntaxKind.SemicolonToken);
        }

        [Fact]
        public void ParseReturnStatementWithExpression()
        {
            var returnStatement = TestUtils.ParseStatement<ReturnStatement>("return (1 + 2) * 4;");
            returnStatement.Should().NotBeNull();
            returnStatement.ReturnKeywordToken.Text.ToString().Should().Be("return");
            returnStatement.ReturnKeywordToken.Kind.Should().Be(SyntaxKind.ReturnKeywordToken);
            returnStatement.SemicolonToken.Text.ToString().Should().Be(";");
            returnStatement.SemicolonToken.Kind.Should().Be(SyntaxKind.SemicolonToken);

            returnStatement.ReturnValueExpression.As<BinaryExpression>().Invoking(binaryExpression =>
            {
                binaryExpression.Left.As<ParethesizedExpression>().InnerExpression.As<BinaryExpression>().Invoking(inner =>
                {
                    inner.Left.As<LiteralExpression>().GetText().Should().Be("1");
                    inner.Operator.Text.ToString().Should().Be("+");
                    inner.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
                    inner.Right.As<LiteralExpression>().GetText().Should().Be("2");
                }).Should().NotThrow();

                binaryExpression.Operator.Text.ToString().Should().Be("*");
                binaryExpression.Operator.Kind.Should().Be(SyntaxKind.StarToken);
                binaryExpression.Right.As<LiteralExpression>().GetText().Should().Be("4");
            }).Should().NotThrow();
        }
    }
}
