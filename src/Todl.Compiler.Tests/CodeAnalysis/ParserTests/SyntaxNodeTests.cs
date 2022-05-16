using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class SyntaxNodeTests
{
    [Theory]
    [MemberData(nameof(GetAllSyntaxNodesForTest))]
    public void TextShouldBeRepeatable(string inputText, SyntaxNode syntaxNode)
    {
        syntaxNode.Text.ToString().Should().Be(inputText);
    }

    [Theory]
    [MemberData(nameof(GetAllSyntaxNodesForTest))]
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public void SyntaxTreePropertyShouldNotBeNull(string inputTextIgnored, SyntaxNode syntaxNode)
    {
        syntaxNode.SyntaxTree.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(GetAllSyntaxNodesForTest))]
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public void DiagnosticBagShouldNotBeNull(string inputTextIgnored, SyntaxNode syntaxNode)
    {
        syntaxNode.GetDiagnostics().Should().NotBeNull();
    }

    [Fact]
    public void AllSyntaxNodeTypesNeedsToBeCoveredInTests()
    {
        var types = GetAllSyntaxNodesForTest().Select(pair => pair[1].GetType());
        var exemptions = (new Type[]
        {
            typeof(Argument),
            typeof(Parameter),
            typeof(SyntaxToken),
            typeof(TypeExpression)
        }).ToHashSet();

        var allSyntaxNodeTypes = typeof(Expression)
            .Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(SyntaxNode))
                && !t.IsAbstract
                && !t.IsGenericType
                && !exemptions.Contains(t))
            .ToHashSet();
        var uncoveredTypes = allSyntaxNodeTypes.Where(t => !types.Contains(t));

        uncoveredTypes.Should().BeEmpty();
    }

    private static readonly string[] testExpressions = new[]
    {
        "System.Uri", // NameExpression
        "a = 5", // AssignmentExpression
        "(a == b)", // ParameterizedExpression
        "-a", // UnaryExpression
        "a + 10", // BinaryExpression
        "System.Console.WriteLine(\"Hello World!\")", // FunctionCallExpression
        "\"Hello World!\"", // LiteralExpression
        "\"abc\".Length", // MemberAccessExpression
        "new object()" // NewExpression
    };

    private static readonly string[] testStatements = new[]
    {
        "{ a = 5; a.ToString(); }", // BlockStatement
        "a = 5;", // ExpressionStatement
        "const a = 5;", // VariableDeclarationStatement
        "return;" // ReturnStatement
    };

    private static readonly string[] testDirectives = new[]
    {
        "import * from System;" // ImportDirective
    };

    private static readonly string[] testMembers = new[]
    {
        "const a = 5;", // VariableDeclarationMember
        "void A() { const a = 5; }" // FunctionDeclarationMember
    };

    public static IEnumerable<object[]> GetAllSyntaxNodesForTest()
    {
        foreach (var inputText in testExpressions)
        {
            var expression = SyntaxTree.ParseExpression(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache);
            yield return new object[] { inputText, expression };
        }

        foreach (var inputText in testStatements)
        {
            var statement = SyntaxTree.ParseStatement(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache);
            yield return new object[] { inputText, statement };
        }

        foreach (var inputText in testDirectives)
        {
            var syntaxTree = SyntaxTree.Parse(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache);
            yield return new object[] { inputText, syntaxTree.Directives[0] };
        }

        foreach (var inputText in testMembers)
        {
            var syntaxTree = SyntaxTree.Parse(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache);
            yield return new object[] { inputText, syntaxTree.Members[0] };
        }
    }
}
