using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis
{
    public sealed partial class ParserTests
    {
        [Fact]
        public void ParseFunctionDeclarationMemberWithSimpleReturnType()
        {
            var function = ParseMember<FunctionDeclarationMember>("int Function() {}");
            function.Should().NotBeNull();
            function.Name.Text.Should().Be("Function");
            function.ReturnType.Text.Should().Be("int");
            function.Parameters.Items.Should().BeEmpty();
            function.Body.InnerStatements.Should().BeEmpty();
        }

        [Fact]
        public void ParseFunctionDeclarationMemberWithQualifiedReturnType()
        {
            var function = ParseMember<FunctionDeclarationMember>("System.Uri Function() {}");
            function.Should().NotBeNull();
            function.Name.Text.Should().Be("Function");
            function.ReturnType.Text.Should().Be("System.Uri");
            function.Parameters.Items.Should().BeEmpty();
            function.Body.InnerStatements.Should().BeEmpty();
        }

        [Fact]
        public void ParseFunctionDeclarationMemberWithArrayReturnType()
        {
            var function = ParseMember<FunctionDeclarationMember>("int[] Function() {}");
            function.Should().NotBeNull();
            function.Name.Text.Should().Be("Function");
            function.ReturnType.Text.Should().Be("int[]");
            function.ReturnType.IsArrayType.Should().BeTrue();
            function.Parameters.Items.Should().BeEmpty();
            function.Body.InnerStatements.Should().BeEmpty();
        }

        [Fact]
        public void ParseFunctionDeclarationMemberWithSingleParameter()
        {
            var function = ParseMember<FunctionDeclarationMember>("void Function(int a) {}");
            function.Should().NotBeNull();
            function.Name.Text.Should().Be("Function");
            function.ReturnType.Text.Should().Be("void");
            function.Parameters.Items.Count.Should().Be(1);
            function.Body.InnerStatements.Should().BeEmpty();

            var a = function.Parameters.Items[0];
            a.ParameterType.Text.Should().Be("int");
            a.Identifier.Text.Should().Be("a");
        }

        [Fact]
        public void ParseFunctionDeclarationMemberWithMultipleParameters()
        {
            var function = ParseMember<FunctionDeclarationMember>("void Function(int a, System.Uri b) {}");
            function.Should().NotBeNull();
            function.Name.Text.Should().Be("Function");
            function.ReturnType.Text.Should().Be("void");
            function.Parameters.Items.Count.Should().Be(2);
            function.Body.InnerStatements.Should().BeEmpty();

            var a = function.Parameters.Items[0];
            a.ParameterType.Text.Should().Be("int");
            a.Identifier.Text.Should().Be("a");

            var b = function.Parameters.Items[1];
            b.ParameterType.Text.Should().Be("System.Uri");
            b.Identifier.Text.Should().Be("b");
        }

        [Fact]
        public void ParseFunctionDeclarationMemberWithArrayParameters()
        {
            var function = ParseMember<FunctionDeclarationMember>("void Function(string[] a, System.Uri[] b) {}");
            function.Should().NotBeNull();
            function.Name.Text.Should().Be("Function");
            function.ReturnType.Text.Should().Be("void");
            function.Parameters.Items.Count.Should().Be(2);
            function.Body.InnerStatements.Should().BeEmpty();

            var a = function.Parameters.Items[0];
            a.ParameterType.Text.Should().Be("string[]");
            a.Identifier.Text.Should().Be("a");
            a.ParameterType.IsArrayType.Should().BeTrue();

            var b = function.Parameters.Items[1];
            b.ParameterType.Text.Should().Be("System.Uri[]");
            b.Identifier.Text.Should().Be("b");
            b.ParameterType.IsArrayType.Should().BeTrue();
        }

        [Fact]
        public void ParseFunctionDeclarationMemberWithMultiDimensionalArrayParameters()
        {
            var function = ParseMember<FunctionDeclarationMember>("void Function(int intParameter, string[] stringArrayParameter, System.Uri[][] twoDimensionalArraysParameter) {}");
            function.Should().NotBeNull();
            function.Name.Text.Should().Be("Function");
            function.ReturnType.Text.Should().Be("void");
            function.Parameters.Items.Count.Should().Be(3);
            function.Body.InnerStatements.Should().BeEmpty();

            var intParameter = function.Parameters.Items[0];
            intParameter.ParameterType.Text.Should().Be("int");
            intParameter.Identifier.Text.Should().Be(nameof(intParameter));
            intParameter.ParameterType.IsArrayType.Should().BeFalse();

            var stringArrayParameter = function.Parameters.Items[1];
            stringArrayParameter.ParameterType.Text.Should().Be("string[]");
            stringArrayParameter.Identifier.Text.Should().Be(nameof(stringArrayParameter));
            stringArrayParameter.ParameterType.IsArrayType.Should().BeTrue();

            var twoDimensionalArraysParameter = function.Parameters.Items[2];
            twoDimensionalArraysParameter.ParameterType.Text.Should().Be("System.Uri[][]");
            twoDimensionalArraysParameter.Identifier.Text.Should().Be(nameof(twoDimensionalArraysParameter));
            twoDimensionalArraysParameter.ParameterType.IsArrayType.Should().BeTrue();
        }

        [Fact]
        public void ParseEmptyReturnStatement()
        {
            var emptyReturnStatement = ParseStatement<ReturnStatement>("return;");
            emptyReturnStatement.Should().NotBeNull();
            emptyReturnStatement.ReturnKeywordToken.Text.Should().Be("return");
            emptyReturnStatement.ReturnKeywordToken.Kind.Should().Be(SyntaxKind.ReturnKeywordToken);
            emptyReturnStatement.ReturnValueExpression.Should().BeNull();
            emptyReturnStatement.SemicolonToken.Text.Should().Be(";");
            emptyReturnStatement.SemicolonToken.Kind.Should().Be(SyntaxKind.SemicolonToken);
        }

        [Fact]
        public void ParseReturnStatementWithExpression()
        {
            var returnStatement = ParseStatement<ReturnStatement>("return (1 + 2) * 4;");
            returnStatement.Should().NotBeNull();
            returnStatement.ReturnKeywordToken.Text.Should().Be("return");
            returnStatement.ReturnKeywordToken.Kind.Should().Be(SyntaxKind.ReturnKeywordToken);
            returnStatement.SemicolonToken.Text.Should().Be(";");
            returnStatement.SemicolonToken.Kind.Should().Be(SyntaxKind.SemicolonToken);

            returnStatement.ReturnValueExpression.As<BinaryExpression>().Invoking(binaryExpression =>
            {
                binaryExpression.Left.As<ParethesizedExpression>().InnerExpression.As<BinaryExpression>().Invoking(inner =>
                {
                    inner.Left.As<LiteralExpression>().Text.Should().Be("1");
                    inner.Operator.Text.Should().Be("+");
                    inner.Operator.Kind.Should().Be(SyntaxKind.PlusToken);
                    inner.Right.As<LiteralExpression>().Text.Should().Be("2");
                });

                binaryExpression.Operator.Text.Should().Be("*");
                binaryExpression.Operator.Kind.Should().Be(SyntaxKind.StarToken);
                binaryExpression.Right.As<LiteralExpression>().Text.Should().Be("4");
            });
        }
    }
}
