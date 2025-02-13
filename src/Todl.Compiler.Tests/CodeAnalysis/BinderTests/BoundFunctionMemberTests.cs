﻿using System;
using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.Diagnostics;
using Xunit;
using Xunit.Sdk;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundFunctionMemberTests
{
    [Theory]
    [InlineData("int Function() {}", typeof(int), 0)]
    [InlineData("System.Uri Function() {}", typeof(Uri), 0)]
    [InlineData("void Function() {}", typeof(void), 1)]
    [InlineData("int[] Function() {}", typeof(int[]), 0)]
    [InlineData("System.Uri[][] Function() {}", typeof(Uri[][]), 0)]
    public void TestBindFunctionDeclarationMemberWithoutParametersOrBody(string inputText, Type expectedReturnType, int expectedStatementsCount)
    {
        var function = TestUtils.BindMember<BoundFunctionMember>(inputText);

        function.Body.Statements.Should().HaveCount(expectedStatementsCount);
        function.ReturnType.Name.Should().Be(expectedReturnType.FullName);
        function.ReturnType.IsArray.Should().Be(expectedReturnType.IsArray);
        function.FunctionScope.BoundScopeKind.Should().Be(BoundScopeKind.Function);
    }

    [Fact]
    public void TestBindFunctionDeclarationMemberWithBody()
    {
        var function = TestUtils.BindMember<BoundFunctionMember>(
            inputText: "void Main() { const a = 30; System.Threading.Thread.Sleep(a); }");

        function.Body.Statements.Should().HaveCount(3);

        var a = function.Body.Statements[0].As<BoundVariableDeclarationStatement>().Variable;
        a.Name.Should().Be("a");
        function.FunctionScope.LookupVariable("a").Should().Be(a);

        function.Body.Statements[1].As<BoundExpressionStatement>().Expression.As<BoundClrFunctionCallExpression>().Should().NotBeNull();

        function.ReturnType.SpecialType.Should().Be(SpecialType.ClrVoid);
        function.FunctionScope.BoundScopeKind.Should().Be(BoundScopeKind.Function);
    }

    [Fact]
    public void TestBindFunctionDeclarationMemberWithParameters()
    {
        var function = TestUtils.BindMember<BoundFunctionMember>(
            inputText: "void Sleep(int a) { System.Threading.Thread.Sleep(a); }");

        var a = function.FunctionScope.LookupVariable("a");
        a.Should().NotBeNull();
        a.Name.Should().Be("a");
        a.Type.SpecialType.Should().Be(SpecialType.ClrInt32);

        function.Body.Statements.Should().HaveCount(2);
        function.Body.Statements[0].As<BoundExpressionStatement>().Expression.As<BoundClrFunctionCallExpression>().Should().NotBeNull();

        function.ReturnType.SpecialType.Should().Be(SpecialType.ClrVoid);
        function.FunctionScope.BoundScopeKind.Should().Be(BoundScopeKind.Function);
    }

    [Fact]
    public void TestBindFunctionDeclarationMemberWithArrayParameters()
    {
        var function = TestUtils.BindMember<BoundFunctionMember>(
            inputText: "void Function(int a, int[] b, string[][] c) { }");

        var a = function.FunctionScope.LookupVariable("a");
        a.Should().NotBeNull();
        a.Name.Should().Be("a");
        a.Type.SpecialType.Should().Be(SpecialType.ClrInt32);
        a.Type.IsArray.Should().BeFalse();

        var b = function.FunctionScope.LookupVariable("b");
        b.Should().NotBeNull();
        b.Name.Should().Be("b");
        b.Type.As<ClrTypeSymbol>().ClrType.GetElementType().Should().Be(TestDefaults.DefaultClrTypeCache.BuiltInTypes.Int32.ClrType);
        b.Type.IsArray.Should().BeTrue();

        var c = function.FunctionScope.LookupVariable("c");
        c.Should().NotBeNull();
        c.Name.Should().Be("c");
        c.Type.As<ClrTypeSymbol>().ClrType.GetElementType().GetElementType().Should().Be(TestDefaults.DefaultClrTypeCache.BuiltInTypes.String.ClrType);
        c.Type.IsArray.Should().BeTrue();

        function.ReturnType.SpecialType.Should().Be(SpecialType.ClrVoid);
        function.FunctionScope.BoundScopeKind.Should().Be(BoundScopeKind.Function);
    }

    [Theory]
    [InlineData("int Function() { return 0; }", typeof(int))]
    [InlineData("System.Uri Function() { return new System.Uri(\"https://www.google.com\"); }", typeof(Uri))]
    [InlineData("void Function() { return; }", typeof(void))]
    public void TestBindFunctionDeclarationMemberWithReturnStatement(string inputText, Type expectedReturnType)
    {
        var function = TestUtils.BindMember<BoundFunctionMember>(inputText);

        var targetType = TestDefaults.DefaultClrTypeCache.Resolve(expectedReturnType.FullName);
        function.Body.Statements.Should().HaveCount(1);
        function.ReturnType.Should().Be(targetType);

        var returnStatement = function.Body.Statements[0].As<BoundReturnStatement>();
        returnStatement.Should().NotBeNull();
        returnStatement.ReturnType.Should().Be(targetType);
    }

    [Theory]
    [InlineData("int Function() { return; }", typeof(int), typeof(void))]
    [InlineData("System.Uri Function() { return \"https://www.google.com\"; }", typeof(Uri), typeof(string))]
    [InlineData("void Function() { return 0; }", typeof(void), typeof(int))]
    public void TestBindFunctionDeclarationMemberWithMismatchedReturnStatement(string inputText, Type expectedReturnType, Type actualReturnType)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var function = TestUtils.BindMember<BoundFunctionMember>(inputText, diagnosticBuilder);

        var resolvedExpectedType = TestDefaults.DefaultClrTypeCache.Resolve(expectedReturnType.FullName);
        var resolvedActualType = TestDefaults.DefaultClrTypeCache.Resolve(actualReturnType.FullName);

        function.Body.Statements.Should().HaveCount(1);
        function.ReturnType.Should().Be(resolvedExpectedType);

        var returnStatement = function.Body.Statements[0].As<BoundReturnStatement>();
        returnStatement.Should().NotBeNull();
        returnStatement.ReturnType.Should().Be(resolvedActualType);

        var diagnostics = diagnosticBuilder.Build().ToList();
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

        TestUtils.BindModule(inputText).Should().NotBeNull();
    }

    [Theory]
    [InlineData("void func(int a, int a) { }")]
    [InlineData("void func(int a, string a) { }")]
    public void FunctionParametersShouldHaveDistinctNames(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindModule(inputText, diagnosticBuilder).Should().NotBeNull();

        var diagnostics = diagnosticBuilder.Build().ToList();
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

        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindModule(inputText, diagnosticBuilder).Should().NotBeNull();

        var diagnostics = diagnosticBuilder.Build().ToList();
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

        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindModule(inputText, diagnosticBuilder).Should().NotBeNull();

        var diagnostics = diagnosticBuilder.Build().ToList();
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

        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindModule(inputText, diagnosticBuilder).Should().NotBeNull();

        var diagnostics = diagnosticBuilder.Build().ToList();
        diagnostics.Count.Should().Be(1);
        diagnostics[0].ErrorCode.Should().Be(ErrorCode.AmbiguousFunctionDeclaration);
    }

    [Theory]
    [InlineData("void func() {}")]
    [InlineData("void func() { return; }")]
    [InlineData("void func(int a) { System.Threading.Thread.Sleep(a); }")]
    [InlineData("void func(int a) { System.Threading.Thread.Sleep(a); return; }")]
    public void FunctionsThatReturnsVoidShouldNotHaveDuplicateReturnStatements(string inputText)
    {
        var function = TestUtils.BindMember<BoundFunctionMember>(inputText);
        function.Body.Statements.Should().NotBeEmpty();
        function.Body.Statements.OfType<BoundReturnStatement>().Should().HaveCount(1);

        var lastStatement = function.Body.Statements[^1];
        lastStatement.Should().BeOfType<BoundReturnStatement>();
    }
}

