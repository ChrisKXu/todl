﻿using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class ConstantFoldingTests
{
    [Theory]
    [InlineData("const a = +10;", 10)]
    [InlineData("const a = +10U;", 10U)]
    [InlineData("const a = +10L;", 10L)]
    [InlineData("const a = +10UL;", 10UL)]
    [InlineData("const a = +1.0F;", 1.0F)]
    [InlineData("const a = +1.0;", 1.0)]
    [InlineData("const a = -10;", -10)]
    [InlineData("const a = -10U;", -10U)]
    [InlineData("const a = -10L;", -10L)]
    [InlineData("const a = -1.0F;", -1.0F)]
    [InlineData("const a = -1.0;", -1.0)]
    [InlineData("const a = !true;", false)]
    [InlineData("const a = !false;", true)]
    [InlineData("const a = ~10;", ~10)]
    [InlineData("const a = ~10U;", ~10U)]
    [InlineData("const a = ~10L;", ~10L)]
    [InlineData("const a = ~10UL;", ~10UL)]
    public void ConstantFoldingUnaryOperatorTest(string inputText, object expectedValue)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var syntaxTree = SyntaxTree.Parse(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache, diagnosticBuilder);
        var module = BoundModule.Create(TestDefaults.DefaultClrTypeCache, [syntaxTree], diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();

        var variableMember = module.EntryPointType.Variables.ToList()[^1].As<BoundVariableMember>();
        variableMember.BoundVariableDeclarationStatement.Variable.Constant.Should().Be(true);
        var value = variableMember
            .BoundVariableDeclarationStatement
            .InitializerExpression
            .As<BoundConstant>()
            .Value;

        value.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("const a = 10 + 10;", 20)]
    [InlineData("const a = 10; const b = a + 10;", 20)]
    [InlineData("const a = 10; const b = a * 2;", 20)]
    [InlineData("const a = true;", true)]
    [InlineData("const a = true; const b = a && false", false)]
    [InlineData("const a = -20;", -20)]
    public void BasicConstantFoldingTests(string inputText, object expectedValue)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var syntaxTree = SyntaxTree.Parse(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache, diagnosticBuilder);
        var module = BoundModule.Create(TestDefaults.DefaultClrTypeCache, [syntaxTree], diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();

        var variableMember = module.EntryPointType.Variables.ToList()[^1].As<BoundVariableMember>();
        variableMember.BoundVariableDeclarationStatement.Variable.Constant.Should().Be(true);
        var value = variableMember
            .BoundVariableDeclarationStatement
            .InitializerExpression
            .As<BoundConstant>()
            .Value;

        value.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("let a = 10 + 10;")]
    [InlineData("const a = 10; let b = a + 10;")]
    [InlineData("const a = 10; let b = a * 2;")]
    [InlineData("const a = 10; let b = a + 10; const c = a + b;")]
    public void BasicConstantFoldingNegativeTests(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var syntaxTree = SyntaxTree.Parse(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache, diagnosticBuilder);
        var module = BoundModule.Create(TestDefaults.DefaultClrTypeCache, [syntaxTree], diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();

        var variableMember = module.EntryPointType.Variables.ToList()[^1].As<BoundVariableMember>();
        var boundVariableDeclarationStatement = variableMember.BoundVariableDeclarationStatement;
        boundVariableDeclarationStatement.Variable.Constant.Should().Be(false);
    }

    [Fact]
    public void PartiallyFoldedConstantTests()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var syntaxTree = SyntaxTree.Parse(SourceText.FromString("let a = 10 + 10;"), TestDefaults.DefaultClrTypeCache, diagnosticBuilder);
        var module = BoundModule.Create(TestDefaults.DefaultClrTypeCache, [syntaxTree], diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();

        var statement = module.EntryPointType.Variables.ToList()[^1].As<BoundVariableMember>().BoundVariableDeclarationStatement;
        statement.Variable.Constant.Should().Be(false);
        statement.InitializerExpression.Constant.Should().Be(true);
        statement.InitializerExpression.As<BoundConstant>().Value.Should().Be(20);
    }
}
