using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundNodeTests
{
    [Theory]
    [MemberData(nameof(GetAllSyntaxNodesForTest))]
    public void BoundNodeShouldHaveCorrectSyntaxNode(SyntaxNode syntaxNode, BoundNode boundNode)
    {
        boundNode.SyntaxNode.Should().NotBeNull();
        boundNode.SyntaxNode.Should().Be(syntaxNode);
    }

    [Theory]
    [MemberData(nameof(GetAllSyntaxNodesForTest))]
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public void DiagnosticBagShouldNotBeNull(SyntaxNode unused, BoundNode boundNode)
    {
        boundNode.DiagnosticBuilder.Should().NotBeNull();
        boundNode.GetDiagnostics().Should().NotBeNull();
    }

    [Fact]
    public void AllBoundNodeVariantsAreCovered()
    {
        var types = GetAllSyntaxNodesForTest().Select(pair => pair[1].GetType());

        var allBoundNodeTypes = typeof(BoundNode)
            .Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BoundNode))
                && !t.IsAbstract)
            .ToHashSet();
        var uncoveredTypes = allBoundNodeTypes.Where(t => !types.Contains(t));

        uncoveredTypes.Should().BeEmpty();
    }

    private static readonly string[] testExpressions = new[]
    {
        "System.Uri", // BoundTypeExpression
        "a = 5", // BoundAssignmentExpression
        "-10", // BoundUnaryExpression
        "System.Int32.MinValue + 10", // BoundBinaryExpression
        "100.ToString()", // BoundFunctionCallExpression
        "\"Hello World!\"", // BoundConstant
        "\"abc\".Length", // BoundMemberAccessExpression
        "new System.Exception()" // BoundNewExpression
    };

    private static readonly string[] testStatements = new[]
    {
        "const a = 10;", // BoundVariableDeclarationStatement
        "a = 10;", // BoundExpressionStatement
        "{ const a = 5; a.ToString(); }", // BoundBlockStatement
        "return 10;" // ReturnStatement
    };

    private static readonly string[] testMembers = new[]
    {
        "void Main() { }" // BoundFunctionMember
    };

    public static IEnumerable<object[]> GetAllSyntaxNodesForTest()
    {
        foreach (var inputText in testExpressions)
        {
            var expression = SyntaxTree.ParseExpression(SourceText.FromString(inputText));
            var binder = Binder.CreateScriptBinder();
            yield return new object[] { expression, binder.BindExpression(expression) };
        }

        // BoundVariableExpression requires special logic to work
        {
            var sourceText = SourceText.FromString("{ const a = 5; a; }");
            var blockStatement = SyntaxTree.ParseStatement(sourceText);
            var binder = Binder.CreateModuleBinder();
            var boundBlockStatement =
                binder.BindStatement(blockStatement).As<BoundBlockStatement>();

            var boundVariableExpression =
                boundBlockStatement.Statements[1].As<BoundExpressionStatement>().Expression.As<BoundVariableExpression>();
            yield return new object[] { boundVariableExpression.SyntaxNode, boundVariableExpression };
        }

        foreach (var inputText in testStatements)
        {
            var statement = SyntaxTree.ParseStatement(SourceText.FromString(inputText));
            var binder = Binder.CreateModuleBinder();
            yield return new object[] { statement, binder.BindStatement(statement) };
        }

        foreach (var inputText in testMembers)
        {
            var syntaxTree = SyntaxTree.Parse(SourceText.FromString(inputText));
            var member = syntaxTree.Members[0];
            var binder = Binder.CreateModuleBinder();
            if (member is FunctionDeclarationMember functionDeclarationMember)
            {
                binder.Scope.DeclareFunction(FunctionSymbol.FromFunctionDeclarationMember(functionDeclarationMember));
            }

            yield return new object[] { member, binder.BindMember(member) };
        }
    }
}
