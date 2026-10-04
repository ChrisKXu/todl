using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundElementAccessExpressionTests
{
    [Fact]
    public void TestBoundArrayElementAccessExpression()
    {
        var boundArrayElementAccessExpression = TestUtils.BindExpression<BoundArrayElementAccessExpression>(
            "System::Environment.GetCommandLineArgs()[0]");

        boundArrayElementAccessExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrString);
        boundArrayElementAccessExpression.LValue.Should().BeTrue();
        boundArrayElementAccessExpression.ReadOnly.Should().BeFalse();
    }

    [Fact]
    public void TestBoundArrayElementAccessExpressionAsAssignmentTarget()
    {
        var boundAssignmentExpression = TestUtils.BindExpression<BoundAssignmentExpression>(
            "System::Environment.GetCommandLineArgs()[0] = \"x\"");

        boundAssignmentExpression.Left.Should().BeOfType<BoundArrayElementAccessExpression>();
    }

    [Fact]
    public void BindArrayElementAccessWithNonIntIndexShouldReportDiagnosticAndReturnInvalidNode()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundExpression = TestUtils.BindExpression<BoundExpression>(
            "System::Environment.GetCommandLineArgs()[\"zero\"]", diagnosticBuilder);

        boundExpression.Should().BeOfType<BoundInvalidElementAccessExpression>();

        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().ContainSingle();
        diagnostics.Single().Level.Should().Be(DiagnosticLevel.Error);
        diagnostics.Single().ErrorCode.Should().Be(ErrorCode.TypeMismatch);
    }

    [Fact]
    public void TestBoundIndexerAccessExpression()
    {
        var boundIndexerAccessExpression = TestUtils.BindExpression<BoundIndexerAccessExpression>("\"abc\"[0]");

        boundIndexerAccessExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrChar);
        boundIndexerAccessExpression.PropertyInfo.Name.Should().Be("Chars");
        boundIndexerAccessExpression.ReadOnly.Should().BeTrue();
        boundIndexerAccessExpression.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void TestBoundWritableIndexerAccessExpression()
    {
        var boundIndexerAccessExpression = TestUtils.BindExpression<BoundIndexerAccessExpression>(
            "new System::Text::StringBuilder(\"abc\")[0]");

        boundIndexerAccessExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrChar);
        boundIndexerAccessExpression.ReadOnly.Should().BeFalse();
    }

    [Fact]
    public void BindIndexerAccessWithNoMatchingIndexerShouldReportDiagnosticAndReturnInvalidNode()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundExpression = TestUtils.BindExpression<BoundExpression>("\"abc\"[\"zero\"]", diagnosticBuilder);

        boundExpression.Should().BeOfType<BoundInvalidElementAccessExpression>();

        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().ContainSingle();
        diagnostics.Single().Level.Should().Be(DiagnosticLevel.Error);
        diagnostics.Single().ErrorCode.Should().Be(ErrorCode.NoMatchingIndexer);
    }

    [Fact]
    public void BindIndexerAccessThroughTypeNameShouldReportDiagnosticAndReturnInvalidNode()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundExpression = TestUtils.BindExpression<BoundExpression>("System::Text::StringBuilder[0]", diagnosticBuilder);

        boundExpression.Should().BeOfType<BoundInvalidElementAccessExpression>();

        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().ContainSingle();
        diagnostics.Single().Level.Should().Be(DiagnosticLevel.Error);
        diagnostics.Single().ErrorCode.Should().Be(ErrorCode.ObjectReferenceRequired);
    }
}
