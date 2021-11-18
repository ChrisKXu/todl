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
    }
}
