using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class ConstantFoldingTests
{
    [Fact]
    public void BasicConstantFoldingTests()
    {
        var inputText = "const a = 10 + 10";
        var syntaxTree = SyntaxTree.Parse(SourceText.FromString(inputText));
        var module = BoundModule.Create(new[] { syntaxTree });
        module.GetDiagnostics().Should().BeEmpty();

        var variableMember = module.BoundMembers[^1].As<BoundVariableMember>();
        variableMember.BoundVariableDeclarationStatement.Variable.Constant.Should().Be(true);
        var value = variableMember
            .BoundVariableDeclarationStatement
            .InitializerExpression
            .As<BoundConstant>()
            .Value;

        value.Should().Be(20);
    }
}
