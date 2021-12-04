using System;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis
{
    public sealed partial class BinderTests
    {
        [Theory]
        [InlineData("int Function() {}", typeof(int))]
        [InlineData("System.Uri Function() {}", typeof(Uri))]
        [InlineData("void Function() {}", typeof(void))]
        public void TestBindFunctionDeclarationMemberWithoutParametersOrBody(string inputText, Type expectedReturnType)
        {
            var function = BindMember<BoundFunctionMember>(inputText);

            function.Body.Statements.Should().BeEmpty();
            function.ReturnType.As<ClrTypeSymbol>().ClrType.Should().Be(expectedReturnType);
            function.FunctionScope.BoundScopeKind.Should().Be(BoundScopeKind.Function);
        }

        [Fact]
        public void TestBindFunctionDeclarationMemberWithBody()
        {
            var function = BindMember<BoundFunctionMember>(
                inputText: "void Main() { const a = 30; System.Threading.Thread.Sleep(a); }");

            function.Body.Statements.Count.Should().Be(2);

            var a = function.Body.Statements[0].As<BoundVariableDeclarationStatement>().Variable;
            a.Name.Should().Be("a");
            function.FunctionScope.LookupVariable("a").Should().Be(a);

            function.Body.Statements[1].As<BoundExpressionStatement>().Expression.As<BoundFunctionCallExpression>().Should().NotBeNull();

            function.ReturnType.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(void));
            function.FunctionScope.BoundScopeKind.Should().Be(BoundScopeKind.Function);
        }

        [Fact]
        public void TestBindFunctionDeclarationMemberWithParameters()
        {
            var function = BindMember<BoundFunctionMember>(
                inputText: "void Sleep(int a) { System.Threading.Thread.Sleep(a); }");

            var a = function.FunctionScope.LookupVariable("a");
            a.Should().NotBeNull();
            a.Name.Should().Be("a");
            a.Type.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(int));

            function.Body.Statements.Count.Should().Be(1);
            function.Body.Statements[0].As<BoundExpressionStatement>().Expression.As<BoundFunctionCallExpression>().Should().NotBeNull();

            function.ReturnType.As<ClrTypeSymbol>().ClrType.Should().Be(typeof(void));
            function.FunctionScope.BoundScopeKind.Should().Be(BoundScopeKind.Function);
        }
    }
}
