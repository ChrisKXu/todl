using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed partial class BinderTests
{
    [Theory]
    [InlineData("if true { 0.ToString(); }", false)]
    [InlineData("unless true { 0.ToString(); }", true)]
    public void TestBindIfUnlessStatementBasic(string inputText, bool inverted)
    {
        var boundConditionalStatement = BindStatement<BoundConditionalStatement>(inputText);
        boundConditionalStatement.Should().NotBeNull();
        boundConditionalStatement.GetDiagnostics().Should().BeEmpty();

        boundConditionalStatement.Condition.As<BoundConstant>().Constant.Should().Be(true);

        var blockStatement = (inverted ? boundConditionalStatement.Alternative : boundConditionalStatement.Consequence).As<BoundBlockStatement>();
        blockStatement.Statements.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("if 0 { 0.ToString(); }")]
    public void TestBindIfUnlessStatementWithNonBooleanConditions(string inputText)
    {
        var boundConditionalStatement = BindStatement<BoundConditionalStatement>(inputText);
        boundConditionalStatement.Should().NotBeNull();

        var diagnostics = boundConditionalStatement.GetDiagnostics().ToList();
        diagnostics[0].ErrorCode.Should().Be(ErrorCode.TypeMismatch);
    }

    [Fact]
    public void BoundConditionalStatementShouldHaveCorrectScope()
    {
        var inputText = @"
            void func() {
                const a = 0;
                let b = a + 4;
                if true {
                    let a = 20;
                    a += 20;
                    b = 20;
                }
                b = a + 5;
            }";
        var syntaxTree = ParseSyntaxTree(inputText);
        var boundModule = BoundModule.Create(TestDefaults.DefaultClrTypeCache, new[] { syntaxTree });
        boundModule.GetDiagnostics().Should().BeEmpty();

        var func = boundModule.EntryPointType.BoundMembers[0].As<BoundFunctionMember>();
        var conditionalStatement = func.Body.Statements[2].As<BoundConditionalStatement>();
        var consequence = conditionalStatement.Consequence.As<BoundBlockStatement>();
        consequence.Scope.Parent.Should().Be(func.FunctionScope);

        var outerA = func.FunctionScope.LookupVariable("a");
        var outerB = func.FunctionScope.LookupVariable("b");
        var innerA = consequence.Scope.LookupVariable("a");
        var innerB = consequence.Scope.LookupVariable("b");

        outerA.Should().NotBe(innerA);
        outerB.Should().Be(innerB);
    }
}
