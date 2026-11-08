using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.Diagnostics;
using Xunit;

using static Todl.Compiler.CodeAnalysis.Binding.BoundTree.BoundAssignmentExpression;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundAssignmentExpressionTests
{
    [Theory]
    [InlineData("{ let n = 0; n = 0; }", "n", BoundAssignmentOperatorKind.Assignment, SpecialType.ClrInt32)]
    [InlineData("{ let abcd = string.Empty; abcd = \"abcde\"; }", "abcd", BoundAssignmentOperatorKind.Assignment, SpecialType.ClrString)]
    [InlineData("{ let n = 0; n += 10; }", "n", BoundAssignmentOperatorKind.AdditionInline, SpecialType.ClrInt32)]
    [InlineData("{ let n = 0; n -= 10; }", "n", BoundAssignmentOperatorKind.SubstractionInline, SpecialType.ClrInt32)]
    [InlineData("{ let n = 0; n *= 10; }", "n", BoundAssignmentOperatorKind.MultiplicationInline, SpecialType.ClrInt32)]
    [InlineData("{ let n = 0; n /= 10; }", "n", BoundAssignmentOperatorKind.DivisionInline, SpecialType.ClrInt32)]
    void TestBindAssignmentExpressionBasic(
        string input,
        string variableName,
        BoundAssignmentOperatorKind expectedBoundAssignmentOperatorKind,
        SpecialType expectedResultType)
    {
        var block = TestUtils.BindStatement<BoundBlockStatement>(input);
        var boundAssignmentExpression = block.Statements[1].As<BoundExpressionStatement>().Expression.As<BoundAssignmentExpression>();
        boundAssignmentExpression.Should().NotBeNull();

        boundAssignmentExpression.Operator.BoundAssignmentOperatorKind.Should().Be(expectedBoundAssignmentOperatorKind);
        boundAssignmentExpression.ResultType.SpecialType.Should().Be(expectedResultType);

        var variable = boundAssignmentExpression.Left.As<BoundVariableExpression>().Variable;

        variable.Name.Should().Be(variableName);
        variable.ReadOnly.Should().BeFalse();
        variable.Type.SpecialType.Should().Be(expectedResultType);
    }

    [Theory]
    [InlineData("{ const n = 0; n = 0; }")]
    [InlineData("{ const n = 0; n += 10; }")]
    [InlineData("{ const n = 0; n -= 10; }")]
    [InlineData("{ const n = 0; n *= 10; }")]
    [InlineData("{ const n = 0; n /= 10; }")]
    public void TestBindAssignmentExpressionWithReadonlyVariables(string input)
    {
        var diagnosticsBuilder = new DiagnosticBag.Builder();
        var block = TestUtils.BindStatement<BoundBlockStatement>(input, diagnosticsBuilder);
        var boundAssignmentExpression = block.Statements[1].As<BoundExpressionStatement>().Expression.As<BoundAssignmentExpression>();
        boundAssignmentExpression.Should().NotBeNull();

        var diagnostics = diagnosticsBuilder.Build();
        diagnostics.Should().HaveCount(1);

        var readonlyVariable = diagnostics.First();
        readonlyVariable.Level.Should().Be(DiagnosticLevel.Error);
        readonlyVariable.ErrorCode.Should().Be(ErrorCode.ReadOnlyVariable);
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
        var diagnosticsBuilder = new DiagnosticBag.Builder();
        var boundAssignmentExpression = TestUtils.BindExpression<BoundAssignmentExpression>(input, diagnosticsBuilder);
        boundAssignmentExpression.Should().NotBeNull();

        var diagnostics = diagnosticsBuilder.Build();
        diagnostics.Should().HaveCount(1);

        var undeclaredVariable = diagnostics.First();
        undeclaredVariable.Level.Should().Be(DiagnosticLevel.Error);
        undeclaredVariable.ErrorCode.Should().Be(ErrorCode.UndeclaredVariable);
        undeclaredVariable.Message.Should().Be("Undeclared variable n");
    }

    [Theory]
    [InlineData("{ let n = 0; n = \"abc\"; }")]
    [InlineData("{ let n = 0; n += \"abc\"; }")]
    [InlineData("{ let n = 0; n -= \"abc\"; }")]
    [InlineData("{ let n = 0; n *= \"abc\"; }")]
    [InlineData("{ let n = 0; n /= \"abc\"; }")]
    public void TestBindAssignmentExpressionWithMismatchedTypes(string input)
    {
        var diagnosticsBuilder = new DiagnosticBag.Builder();
        var block = TestUtils.BindStatement<BoundBlockStatement>(input, diagnosticsBuilder);
        var boundAssignmentExpression = block.Statements[1].As<BoundExpressionStatement>().Expression.As<BoundAssignmentExpression>();
        boundAssignmentExpression.Should().NotBeNull();

        var diagnostics = diagnosticsBuilder.Build();
        diagnostics.Should().HaveCount(1);

        var typeMismatch = diagnostics.First();
        typeMismatch.Level.Should().Be(DiagnosticLevel.Error);
        typeMismatch.ErrorCode.Should().Be(ErrorCode.TypeMismatch);
        typeMismatch.Message.Should().Be("Variable n cannot be assigned to type System.String");
    }

    [Theory]
    [InlineData("{ let a = [1, 2, 3]; a[0] = \"x\"; }")]
    [InlineData("{ let a = [1, 2, 3]; a[0] += \"x\"; }")]
    public void TestBindAssignmentExpressionToArrayElementWithMismatchedTypes(string input)
    {
        var diagnosticsBuilder = new DiagnosticBag.Builder();
        var block = TestUtils.BindStatement<BoundBlockStatement>(input, diagnosticsBuilder);
        var boundAssignmentExpression = block.Statements[1].As<BoundExpressionStatement>().Expression.As<BoundAssignmentExpression>();

        boundAssignmentExpression.Left.Should().BeOfType<BoundArrayElementAccessExpression>();

        var typeMismatch = diagnosticsBuilder.Build().Should().ContainSingle().Subject;
        typeMismatch.Level.Should().Be(DiagnosticLevel.Error);
        typeMismatch.ErrorCode.Should().Be(ErrorCode.TypeMismatch);
        typeMismatch.Message.Should().Be("Array element cannot be assigned to type System.String");
    }

    [Theory]
    [InlineData("Todl::Compiler::Tests::TestClass.Instance.PublicIntField = \"x\"")]
    [InlineData("Todl::Compiler::Tests::TestClass.Instance.PublicIntProperty = \"x\"")]
    [InlineData("Todl::Compiler::Tests::TestClass.Instance[0] = \"x\"")]
    public void TestBindAssignmentExpressionToMemberTargetsWithMismatchedTypes(string input)
    {
        var diagnosticsBuilder = new DiagnosticBag.Builder();
        var boundAssignmentExpression = TestUtils.BindExpression<BoundAssignmentExpression>(input, diagnosticsBuilder);

        boundAssignmentExpression.Should().NotBeNull();

        var typeMismatch = diagnosticsBuilder.Build().Should().ContainSingle().Subject;
        typeMismatch.Level.Should().Be(DiagnosticLevel.Error);
        typeMismatch.ErrorCode.Should().Be(ErrorCode.TypeMismatch);
    }

    [Theory]
    [InlineData("\"abc\".Length = 5", "Member Length is read-only")]
    [InlineData("System::Int32.MaxValue = 5", "Member MaxValue is read-only")]
    [InlineData("\"abc\"[0] = \"x\"", "Indexer of type System.String is read-only")]
    public void TestBindAssignmentExpressionToReadOnlyTargets(string input, string expectedMessage)
    {
        var diagnosticsBuilder = new DiagnosticBag.Builder();
        TestUtils.BindExpression<BoundAssignmentExpression>(input, diagnosticsBuilder);

        var readOnly = diagnosticsBuilder.Build().Should().ContainSingle().Subject;
        readOnly.Level.Should().Be(DiagnosticLevel.Error);
        readOnly.ErrorCode.Should().Be(ErrorCode.ReadOnlyVariable);
        readOnly.Message.Should().Be(expectedMessage);
    }

    [Fact]
    public void TestBindAssignmentExpressionToWritableIndexer()
    {
        var boundAssignmentExpression = TestUtils.BindExpression<BoundAssignmentExpression>(
            "Todl::Compiler::Tests::TestClass.Instance[0] = 10");

        boundAssignmentExpression.Left.Should().BeOfType<BoundIndexerAccessExpression>();
    }

    [Fact]
    public void TestBindAssignmentExpressionAllowsReferenceConversion()
    {
        var boundAssignmentExpression = TestUtils.BindExpression<BoundAssignmentExpression>(
            "Todl::Compiler::Tests::TestClass.Instance.PublicObjectField = \"x\"");

        boundAssignmentExpression.Left.Should().BeOfType<BoundClrFieldAccessExpression>();
    }

    [Fact]
    public void TestBindAssignmentExpressionRejectsBoxingAssignment()
    {
        var diagnosticsBuilder = new DiagnosticBag.Builder();
        TestUtils.BindExpression<BoundAssignmentExpression>(
            "Todl::Compiler::Tests::TestClass.Instance.PublicObjectField = 5", diagnosticsBuilder);

        var typeMismatch = diagnosticsBuilder.Build().Should().ContainSingle().Subject;
        typeMismatch.Level.Should().Be(DiagnosticLevel.Error);
        typeMismatch.ErrorCode.Should().Be(ErrorCode.TypeMismatch);
        typeMismatch.Message.Should().Be("Member PublicObjectField cannot be assigned to type System.Int32");
    }
}
