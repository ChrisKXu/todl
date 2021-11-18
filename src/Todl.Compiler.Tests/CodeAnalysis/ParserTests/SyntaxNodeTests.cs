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

    [Fact]
    public void AllSyntaxNodeTypesNeedsToBeCoveredInTests()
    {
        var types = GetAllSyntaxNodesForTest().Select(pair => pair[1].GetType());
        var exemptions = (new Type[]
        {
            typeof(Argument),
            typeof(Parameter),
            typeof(SyntaxToken)
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
        "const a = 5;" // VariableDeclarationStatement
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
            yield return new object[] { inputText, GetParserForText(inputText).ParseExpression() };
        }

        foreach (var inputText in testStatements)
        {
            yield return new object[] { inputText, GetParserForText(inputText).ParseStatement() };
        }

        foreach (var inputText in testDirectives)
        {
            yield return new object[] { inputText, GetParserForText(inputText).ParseDirective() };
        }

        foreach (var inputText in testMembers)
        {
            yield return new object[] { inputText, GetParserForText(inputText).ParseMember() };
        }
    }

    private static Parser GetParserForText(string sourceText)
    {
        var syntaxTree = new SyntaxTree(SourceText.FromString(sourceText));
        syntaxTree.Lex();
        var parser = new Parser(syntaxTree);

        return parser;
    }
}
