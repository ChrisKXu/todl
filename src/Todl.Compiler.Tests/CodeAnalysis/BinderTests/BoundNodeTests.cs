﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;
using Xunit;

using Binder = Todl.Compiler.CodeAnalysis.Binding.BoundTree.Binder;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundNodeTests
{
    [Theory]
    [MemberData(nameof(GetAllSyntaxNodesForTest))]
    internal void BoundNodeShouldHaveCorrectSyntaxNode(SyntaxNode syntaxNode, BoundNode boundNode)
    {
        boundNode.SyntaxNode.Should().NotBeNull();
        boundNode.SyntaxNode.Should().Be(syntaxNode);
    }

    [Fact]
    public void AllBoundNodeVariantsAreCovered()
    {
        var types = GetAllSyntaxNodesForTest().Select(pair => pair[1].GetType());
        var exceptions = new[] { typeof(BoundEntryPointTypeDefinition), typeof(BoundNoOpStatement), typeof(BoundInvalidMemberAccessExpression) };

        var allBoundNodeTypes = typeof(BoundNode)
            .Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BoundNode))
                && !t.IsAbstract)
            .ToHashSet();
        var uncoveredTypes = allBoundNodeTypes.Where(t => !types.Contains(t) && !exceptions.Contains(t));

        uncoveredTypes.Should().BeEmpty();
    }

    [Fact]
    public void AllLeafBoundNodeTypesAreDecorated()
    {
        var allBoundNodeTypes = typeof(BoundNode)
            .Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BoundNode))
                && !t.IsAbstract)
            .ToHashSet();
        var exceptions = new[] { typeof(BoundModule), typeof(BoundEntryPointTypeDefinition) };

        allBoundNodeTypes
            .Except(exceptions)
            .Should()
            .NotContain(t => t.GetCustomAttribute<BoundNodeAttribute>() == null);

        allBoundNodeTypes
            .Should()
            .OnlyContain(t => t.IsSealed);
    }

    // Taken from https://github.com/dotnet/roslyn/blob/main/docs/compilers/Design/Bound%20Node%20Design.md
    [Fact]
    public void AllBoundNodeTypesAreEitherAbstractOrSealed()
    {
        typeof(BoundNode)
            .Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BoundNode)))
            .Should()
            .NotContain(t => !t.IsAbstract && !t.IsSealed);
    }

    [Theory]
    [MemberData(nameof(GetAllSyntaxNodesForTest))]
    internal void AllBoundNodeTypesHaveWalkerAndRewriterImplemented(SyntaxNode _, BoundNode boundNode)
    {
        var walker = new TestBoundTreeWalker();
        var rewriter = new TestBoundTreeRewriter();

        boundNode.Accept(walker).Should().Be(boundNode);
        boundNode.Accept(rewriter).Should().Be(boundNode);
    }

    private static readonly string[] testExpressions =
    [
        "System.Uri", // BoundTypeExpression
        "a = 5", // BoundAssignmentExpression
        "-10", // BoundUnaryExpression
        "System.Int32.MinValue + 10", // BoundBinaryExpression
        "100.ToString()", // BoundClrFunctionCallExpression
        "func()", // BoundTodlFunctionCallExpression
        "\"Hello World!\"", // BoundConstant
        "\"abc\".Length", // BoundClrPropertyAccessExpression
        "int.MaxValue", // BoundClrFieldAccessExpression
        "new System.Exception()" // BoundNewExpression
    ];

    private static readonly string[] testStatements =
    [
        "const a = 10;", // BoundVariableDeclarationStatement
        "a = 10;", // BoundExpressionStatement
        "{ const a = 5; a.ToString(); }", // BoundBlockStatement
        "return 10;", // ReturnStatement,
        "if true { }", // IfUnlessStatement
        "break;", // BreakStatement
        "continue;", // ContinueStatement
        "while true { }" // WhileUntilStatement
    ];

    private static readonly string[] testMembers = new[]
    {
        "void Main() { }", // BoundFunctionMember
        "const globalVariable = 10.5;" // BoundVariableMember
    };

    public static IEnumerable<object[]> GetAllSyntaxNodesForTest()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();

        foreach (var inputText in testExpressions)
        {
            var expression = SyntaxTree.ParseExpression(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache, diagnosticBuilder);
            var binder = Binder.CreateScriptBinder(TestDefaults.DefaultClrTypeCache, diagnosticBuilder);
            yield return new object[] { expression, binder.BindExpression(expression) };
        }

        // BoundVariableExpression requires special logic to work
        {
            var sourceText = SourceText.FromString("{ const a = 5; a; }");
            var blockStatement = SyntaxTree.ParseStatement(sourceText, TestDefaults.DefaultClrTypeCache, diagnosticBuilder);
            var binder = Binder.CreateModuleBinder(TestDefaults.DefaultClrTypeCache, TestDefaults.ConstantValueFactory, diagnosticBuilder);
            var boundBlockStatement =
                binder.BindStatement(blockStatement).As<BoundBlockStatement>();

            var boundVariableExpression =
                boundBlockStatement.Statements[1].As<BoundExpressionStatement>().Expression.As<BoundVariableExpression>();
            yield return new object[] { boundVariableExpression.SyntaxNode, boundVariableExpression };
        }

        foreach (var inputText in testStatements)
        {
            var statement = SyntaxTree.ParseStatement(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache, diagnosticBuilder);
            var binder = Binder.CreateModuleBinder(TestDefaults.DefaultClrTypeCache, TestDefaults.ConstantValueFactory, diagnosticBuilder);
            yield return new object[] { statement, binder.BindStatement(statement) };
        }

        foreach (var inputText in testMembers)
        {
            var syntaxTree = SyntaxTree.Parse(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache, diagnosticBuilder);
            var member = syntaxTree.Members[0];
            var binder = Binder.CreateModuleBinder(TestDefaults.DefaultClrTypeCache, TestDefaults.ConstantValueFactory, diagnosticBuilder);
            if (member is FunctionDeclarationMember functionDeclarationMember)
            {
                binder.Scope.DeclareFunction(FunctionSymbol.FromFunctionDeclarationMember(functionDeclarationMember));
            }

            yield return new object[] { member, binder.BindMember(member) };
        }
    }

    private sealed class TestBoundTreeWalker : BoundTreeWalker
    {
        public override BoundNode DefaultVisit(BoundNode node) => default;
    }

    private sealed class TestBoundTreeRewriter : BoundTreeRewriter
    {
        public override BoundNode DefaultVisit(BoundNode node) => default;
    }
}
