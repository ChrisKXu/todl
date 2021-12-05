using System;
using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.Diagnostics;
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

        [Theory]
        [InlineData("int Function() { return 0; }", typeof(int))]
        [InlineData("System.Uri Function() { return new System.Uri(\"https://www.google.com\"); }", typeof(Uri))]
        [InlineData("void Function() { return; }", typeof(void))]
        public void TestBindFunctionDeclarationMemberWithReturnStatement(string inputText, Type expectedReturnType)
        {
            var function = BindMember<BoundFunctionMember>(inputText);

            function.Body.Statements.Count.Should().Be(1);
            function.ReturnType.As<ClrTypeSymbol>().ClrType.Should().Be(expectedReturnType);
            function.GetDiagnostics().Should().BeEmpty();

            var returnStatement = function.Body.Statements[0].As<BoundReturnStatement>();
            returnStatement.Should().NotBeNull();
            returnStatement.GetDiagnostics().Should().BeEmpty();
            returnStatement.ReturnType.As<ClrTypeSymbol>().ClrType.Should().Be(expectedReturnType);
        }

        [Theory]
        [InlineData("int Function() { return; }", typeof(int), typeof(void))]
        [InlineData("System.Uri Function() { return \"https://www.google.com\"; }", typeof(Uri), typeof(string))]
        [InlineData("void Function() { return 0; }", typeof(void), typeof(int))]
        public void TestBindFunctionDeclarationMemberWithMismatchedReturnStatement(string inputText, Type expectedReturnType, Type actualReturnType)
        {
            var function = BindMember<BoundFunctionMember>(inputText);

            function.Body.Statements.Count.Should().Be(1);
            function.ReturnType.As<ClrTypeSymbol>().ClrType.Should().Be(expectedReturnType);
            function.GetDiagnostics().Should().NotBeEmpty();

            var returnStatement = function.Body.Statements[0].As<BoundReturnStatement>();
            returnStatement.Should().NotBeNull();
            returnStatement.GetDiagnostics().Should().NotBeEmpty();
            returnStatement.ReturnType.As<ClrTypeSymbol>().ClrType.Should().Be(actualReturnType);

            var diagnostics = function.GetDiagnostics().ToList();
            diagnostics.Count.Should().Be(1);
            diagnostics[0].ErrorCode.Should().Be(ErrorCode.TypeMismatch);
            diagnostics[0].Message.Should().Be($"The function expects a return type of {expectedReturnType} but {actualReturnType} is returned.");
        }
    }
}
