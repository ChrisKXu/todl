using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class ConstantFoldingTests
{
    [Theory]
    [InlineData("const a = 10 + 10;", 20)]
    [InlineData("const a = 10; const b = a + 10;", 20)]
    [InlineData("const a = 10; const b = a * 2;", 20)]
    [InlineData("const a = true;", true)]
    [InlineData("const a = true; const b = a && false", false)]
    public void BasicConstantFoldingTests(string inputText, object expectedValue)
    {
        var syntaxTree = SyntaxTree.Parse(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache);
        var module = BoundModule.Create(TestDefaults.DefaultClrTypeCache, new[] { syntaxTree });
        module.GetDiagnostics().Should().BeEmpty();

        var variableMember = module.BoundMembers[^1].As<BoundVariableMember>();
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
        var syntaxTree = SyntaxTree.Parse(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache);
        var module = BoundModule.Create(TestDefaults.DefaultClrTypeCache, new[] { syntaxTree });
        module.GetDiagnostics().Should().BeEmpty();

        var variableMember = module.BoundMembers[^1].As<BoundVariableMember>();
        var boundVariableDeclarationStatement = variableMember.BoundVariableDeclarationStatement;
        boundVariableDeclarationStatement.Variable.Constant.Should().Be(false);
    }

    [Fact]
    public void PartiallyFoldedConstantTests()
    {
        var syntaxTree = SyntaxTree.Parse(SourceText.FromString("let a = 10 + 10;"), TestDefaults.DefaultClrTypeCache);
        var module = BoundModule.Create(TestDefaults.DefaultClrTypeCache, new[] { syntaxTree });
        module.GetDiagnostics().Should().BeEmpty();

        var statement = module.BoundMembers[^1].As<BoundVariableMember>().BoundVariableDeclarationStatement;
        statement.Variable.Constant.Should().Be(false);
        statement.InitializerExpression.Constant.Should().Be(true);
        statement.InitializerExpression.As<BoundConstant>().Value.Should().Be(20);
    }
}
