using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.Diagnostics;
using Xunit;

using static Todl.Compiler.CodeAnalysis.Binding.BoundAssignmentExpression;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundAssignmentExpressionTests
{
    [Theory]
    [InlineData("{ let n = 0; n = 0 }", "n", BoundAssignmentOperatorKind.Assignment, SpecialType.ClrInt32)]
    [InlineData("{ let abcd = string.Empty; abcd = \"abcde\" }", "abcd", BoundAssignmentOperatorKind.Assignment, SpecialType.ClrString)]
    [InlineData("{ let n = 0; n += 10 }", "n", BoundAssignmentOperatorKind.AdditionInline, SpecialType.ClrInt32)]
    [InlineData("{ let n = 0; n -= 10 }", "n", BoundAssignmentOperatorKind.SubstractionInline, SpecialType.ClrInt32)]
    [InlineData("{ let n = 0; n *= 10 }", "n", BoundAssignmentOperatorKind.MultiplicationInline, SpecialType.ClrInt32)]
    [InlineData("{ let n = 0; n /= 10 }", "n", BoundAssignmentOperatorKind.DivisionInline, SpecialType.ClrInt32)]
    public void TestBindAssignmentExpressionBasic(
        string input,
        string variableName,
        BoundAssignmentOperatorKind expectedBoundAssignmentOperatorKind,
        SpecialType expectedResultType)
    {
        var block = TestUtils.BindStatement<BoundBlockStatement>(input);
        var boundAssignmentExpression = block.Statements[1].As<BoundExpressionStatement>().Expression.As<BoundAssignmentExpression>();
        boundAssignmentExpression.Should().NotBeNull();
        boundAssignmentExpression.GetDiagnostics().Should().BeEmpty();

        boundAssignmentExpression.Operator.BoundAssignmentOperatorKind.Should().Be(expectedBoundAssignmentOperatorKind);
        boundAssignmentExpression.ResultType.SpecialType.Should().Be(expectedResultType);

        var variable = boundAssignmentExpression.Left.As<BoundVariableExpression>().Variable;

        variable.Name.Should().Be(variableName);
        variable.ReadOnly.Should().BeFalse();
        variable.Type.SpecialType.Should().Be(expectedResultType);
    }

    [Theory]
    [InlineData("{ const n = 0; n = 0 }")]
    [InlineData("{ const n = 0; n += 10 }")]
    [InlineData("{ const n = 0; n -= 10 }")]
    [InlineData("{ const n = 0; n *= 10 }")]
    [InlineData("{ const n = 0; n /= 10 }")]
    public void TestBindAssignmentExpressionWithReadonlyVariables(string input)
    {
        var block = TestUtils.BindStatement<BoundBlockStatement>(input);
        var boundAssignmentExpression = block.Statements[1].As<BoundExpressionStatement>().Expression.As<BoundAssignmentExpression>();
        boundAssignmentExpression.Should().NotBeNull();

        var diagnostics = boundAssignmentExpression.GetDiagnostics();
        diagnostics.Should().HaveCount(1);

        var readonlyVariable = diagnostics.First();
        readonlyVariable.Level.Should().Be(DiagnosticLevel.Error);
        readonlyVariable.Message.Should().Be("Variable n is read-only");
    }

    [Theory]
    [InlineData("n = 0;")]
    [InlineData("n += 10;")]
    [InlineData("n -= 10;")]
    [InlineData("n *= 10;")]
    [InlineData("n /= 10;")]
    public void TestBindAssignmentExpressionWithUndeclaredVariables(string input)
    {
        var boundAssignmentExpression = TestUtils.BindExpression<BoundAssignmentExpression>(input);
        boundAssignmentExpression.Should().NotBeNull();

        var diagnostics = boundAssignmentExpression.GetDiagnostics();
        diagnostics.Should().HaveCount(1);

        var undeclaredVariable = diagnostics.First();
        undeclaredVariable.Level.Should().Be(DiagnosticLevel.Error);
        undeclaredVariable.Message.Should().Be("Undeclared variable n");
    }
}
