using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
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
        boundNode.Should().NotBeOfType<BoundErrorExpression>();
        boundNode.SyntaxNode.Should().NotBeNull();
        boundNode.SyntaxNode.Should().Be(syntaxNode);
    }

    [Fact]
    public void AllBoundNodeVariantsAreCovered()
    {
        var types = GetAllSyntaxNodesForTest().Select(pair => pair[1].GetType());
        var exemptions = (new Type[]
        {
            typeof(BoundErrorExpression)
        }).ToHashSet();

        var allBoundNodeTypes = typeof(BoundNode)
            .Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BoundNode))
                && !t.IsAbstract
                && !exemptions.Contains(t))
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
        "{ const a = 5; a.ToString(); }" // BoundBlockStatement
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
            var binder = new Binder(BinderFlags.AllowVariableDeclarationInAssignment);
            yield return new object[] { expression, binder.BindExpression(BoundScope.GlobalScope, expression) };
        }

        // BoundVariableExpression requires special logic to work
        {
            var sourceText = SourceText.FromString("{ const a = 5; a; }");
            var blockStatement = SyntaxTree.ParseStatement(sourceText);
            var binder = new Binder(BinderFlags.None);
            var boundBlockStatement =
                binder.BindStatement(BoundScope.GlobalScope, blockStatement).As<BoundBlockStatement>();

            var boundVariableExpression =
                boundBlockStatement.Statements[1].As<BoundExpressionStatement>().Expression.As<BoundVariableExpression>();
            yield return new object[] { boundVariableExpression.SyntaxNode, boundVariableExpression };
        }

        foreach (var inputText in testStatements)
        {
            var statement = SyntaxTree.ParseStatement(SourceText.FromString(inputText));
            var binder = new Binder(BinderFlags.None);
            yield return new object[] { statement, binder.BindStatement(BoundScope.GlobalScope, statement) };
        }

        foreach (var inputText in testMembers)
        {
            var syntaxTree = SyntaxTree.Parse(SourceText.FromString(inputText));
            var member = syntaxTree.Members[0];
            var binder = new Binder(BinderFlags.None);
            yield return new object[] { member, binder.BindMember(BoundScope.GlobalScope, member) };
        }
    }
}
