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
        [InlineData("int Function() {}", typeof(int), 0)]
        [InlineData("System.Uri Function() {}", typeof(Uri), 0)]
        [InlineData("void Function() {}", typeof(void), 1)]
        public void TestBindFunctionDeclarationMemberWithoutParametersOrBody(string inputText, Type expectedReturnType, int expectedStatementsCount)
        {
            var function = BindMember<BoundFunctionMember>(inputText);

            var targetType = TestDefaults.DefaultClrTypeCache.Resolve(expectedReturnType.FullName);
            function.Body.Statements.Count.Should().Be(expectedStatementsCount);
            function.ReturnType.Should().Be(targetType);
            function.FunctionScope.BoundScopeKind.Should().Be(BoundScopeKind.Function);
        }

        [Fact]
        public void TestBindFunctionDeclarationMemberWithBody()
        {
            var function = BindMember<BoundFunctionMember>(
                inputText: "void Main() { const a = 30; System.Threading.Thread.Sleep(a); }");

            function.Body.Statements.Count.Should().Be(3);

            var a = function.Body.Statements[0].As<BoundVariableDeclarationStatement>().Variable;
            a.Name.Should().Be("a");
            function.FunctionScope.LookupVariable("a").Should().Be(a);

            function.Body.Statements[1].As<BoundExpressionStatement>().Expression.As<BoundClrFunctionCallExpression>().Should().NotBeNull();

            function.ReturnType.Should().Be(builtInTypes.Void);
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
            a.Type.Should().Be(builtInTypes.Int32);

            function.Body.Statements.Count.Should().Be(2);
            function.Body.Statements[0].As<BoundExpressionStatement>().Expression.As<BoundClrFunctionCallExpression>().Should().NotBeNull();

            function.ReturnType.Should().Be(builtInTypes.Void);
            function.FunctionScope.BoundScopeKind.Should().Be(BoundScopeKind.Function);
        }

        [Theory]
        [InlineData("int Function() { return 0; }", typeof(int))]
        [InlineData("System.Uri Function() { return new System.Uri(\"https://www.google.com\"); }", typeof(Uri))]
        [InlineData("void Function() { return; }", typeof(void))]
        public void TestBindFunctionDeclarationMemberWithReturnStatement(string inputText, Type expectedReturnType)
        {
            var function = BindMember<BoundFunctionMember>(inputText);

            var targetType = TestDefaults.DefaultClrTypeCache.Resolve(expectedReturnType.FullName);
            function.Body.Statements.Count.Should().Be(1);
            function.ReturnType.Should().Be(targetType);
            function.GetDiagnostics().Should().BeEmpty();

            var returnStatement = function.Body.Statements[0].As<BoundReturnStatement>();
            returnStatement.Should().NotBeNull();
            returnStatement.GetDiagnostics().Should().BeEmpty();
            returnStatement.ReturnType.Should().Be(targetType);
        }

        [Theory]
        [InlineData("int Function() { return; }", typeof(int), typeof(void))]
        [InlineData("System.Uri Function() { return \"https://www.google.com\"; }", typeof(Uri), typeof(string))]
        [InlineData("void Function() { return 0; }", typeof(void), typeof(int))]
        public void TestBindFunctionDeclarationMemberWithMismatchedReturnStatement(string inputText, Type expectedReturnType, Type actualReturnType)
        {
            var function = BindMember<BoundFunctionMember>(inputText);

            var resolvedExpectedType = TestDefaults.DefaultClrTypeCache.Resolve(expectedReturnType.FullName);
            var resolvedActualType = TestDefaults.DefaultClrTypeCache.Resolve(actualReturnType.FullName);

            function.Body.Statements.Count.Should().Be(1);
            function.ReturnType.Should().Be(resolvedExpectedType);
            function.GetDiagnostics().Should().NotBeEmpty();

            var returnStatement = function.Body.Statements[0].As<BoundReturnStatement>();
            returnStatement.Should().NotBeNull();
            returnStatement.GetDiagnostics().Should().NotBeEmpty();
            returnStatement.ReturnType.Should().Be(resolvedActualType);

            var diagnostics = function.GetDiagnostics().ToList();
            diagnostics.Count.Should().Be(1);
            diagnostics[0].ErrorCode.Should().Be(ErrorCode.TypeMismatch);
            diagnostics[0].Message.Should().Be($"The function expects a return type of {expectedReturnType} but {actualReturnType} is returned.");
        }

        [Fact]
        public void TestOverloadedFunctionDeclarationMember()
        {
            var inputText = @"
                int func() { return 20; }
                int func(int a) { return a; }
            ";
            var syntaxTree = ParseSyntaxTree(inputText);
            var boundModule = BoundModule.Create(TestDefaults.DefaultClrTypeCache, new[] { syntaxTree });

            boundModule.GetDiagnostics().Should().BeEmpty();
        }

        [Theory]
        [InlineData("void func(int a, int a) { }")]
        [InlineData("void func(int a, string a) { }")]
        public void FunctionParametersShouldHaveDistinctNames(string inputText)
        {
            var syntaxTree = ParseSyntaxTree(inputText);
            var boundModule = BoundModule.Create(TestDefaults.DefaultClrTypeCache, new[] { syntaxTree });

            var diagnostics = boundModule.GetDiagnostics().ToList();
            diagnostics.Should().NotBeEmpty();
            diagnostics[0].ErrorCode.Should().Be(ErrorCode.DuplicateParameterName);
        }

        [Fact]
        public void FunctionsWithSameArgumentsShouldBeAmbiguous()
        {
            var inputText = @"
                int func(int a, string b) { return b.Length + a; }
                int func(int a, string b) { return b.Length + a + 1; }
            ";
            var syntaxTree = ParseSyntaxTree(inputText);
            var boundModule = BoundModule.Create(TestDefaults.DefaultClrTypeCache, new[] { syntaxTree });

            var diagnostics = boundModule.GetDiagnostics().ToList();
            diagnostics.Count.Should().Be(1);
            diagnostics[0].ErrorCode.Should().Be(ErrorCode.AmbiguousFunctionDeclaration);
        }

        [Fact]
        public void FunctionsWithSameArgumentsInDifferentOrderShouldBeAmbiguous()
        {
            // the following function declarations are ambiguous
            // considering a function call expression like this: func(a: 10, b: "abc")
            // in C# this is permitted since it's ok if you stick with positional arguments
            // but in todl I would like to avoid potential ambiguity from function declaration
            var inputText = @"
                int func(int a, string b) { return b.Length + a; }
                int func(string b, int a) { return b.Length + a + 1; }
            ";
            var syntaxTree = ParseSyntaxTree(inputText);
            var boundModule = BoundModule.Create(TestDefaults.DefaultClrTypeCache, new[] { syntaxTree });

            var diagnostics = boundModule.GetDiagnostics().ToList();
            diagnostics.Count.Should().Be(1);
            diagnostics[0].ErrorCode.Should().Be(ErrorCode.AmbiguousFunctionDeclaration);
        }

        [Fact]
        public void FunctionsWithSameArgumentsButDifferentNamesShouldBeAmbiguous()
        {
            var inputText = @"
                int func(int a, string b) { return b.Length + a; }
                int func(int b, string a) { return a.Length + b + 1; }
            ";
            var syntaxTree = ParseSyntaxTree(inputText);
            var boundModule = BoundModule.Create(TestDefaults.DefaultClrTypeCache, new[] { syntaxTree });

            var diagnostics = boundModule.GetDiagnostics().ToList();
            diagnostics.Count.Should().Be(1);
            diagnostics[0].ErrorCode.Should().Be(ErrorCode.AmbiguousFunctionDeclaration);
        }

        [Theory]
        [InlineData("void func() {}")]
        [InlineData("void func() { return; }")]
        [InlineData("void func() { System.Threading.Thread.Sleep(a); }")]
        [InlineData("void func() { System.Threading.Thread.Sleep(a); return; }")]
        public void FunctionsThatReturnsVoidShouldNotHaveDuplicateReturnStatements(string inputText)
        {
            var function = BindMember<BoundFunctionMember>(inputText);
            function.Body.Statements.Should().NotBeEmpty();

            function.Body.Statements.OfType<BoundReturnStatement>().Should().HaveCount(1);

            var lastStatement = function.Body.Statements[^1];
            lastStatement.Should().BeOfType<BoundReturnStatement>();
        }
    }
}
