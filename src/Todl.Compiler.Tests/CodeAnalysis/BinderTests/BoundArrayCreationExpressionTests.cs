using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundArrayCreationExpressionTests
{
    [Fact]
    public void TestBoundArrayCreationExpressionBasic()
    {
        var boundArrayCreationExpression = TestUtils.BindExpression<BoundArrayCreationExpression>("new int[5]");

        boundArrayCreationExpression.ResultType.Should().BeOfType<ClrTypeSymbol>();
        ((ClrTypeSymbol)boundArrayCreationExpression.ResultType).ClrType.FullName.Should().Be("System.Int32[]");
        boundArrayCreationExpression.BoundLengthExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrInt32);
    }

    [Fact]
    public void TestBoundJaggedArrayCreationExpression()
    {
        var boundArrayCreationExpression = TestUtils.BindExpression<BoundArrayCreationExpression>("new int[3][]");

        ((ClrTypeSymbol)boundArrayCreationExpression.ResultType).ClrType.FullName.Should().Be("System.Int32[][]");
    }

    [Fact]
    public void BindArrayCreationWithNonIntLengthShouldReportDiagnostic()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindExpression<BoundArrayCreationExpression>("new int[\"five\"]", diagnosticBuilder);

        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().ContainSingle();
        diagnostics.Single().Level.Should().Be(DiagnosticLevel.Error);
        diagnostics.Single().ErrorCode.Should().Be(ErrorCode.TypeMismatch);
    }

    [Fact]
    public void BindArrayCreationWithVoidElementTypeShouldReportDiagnosticAndReturnNullResultType()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundExpression = TestUtils.BindExpression<BoundArrayCreationExpression>("new void[5]", diagnosticBuilder);

        boundExpression.ResultType.Should().BeNull();

        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().ContainSingle();
        diagnostics.Single().ErrorCode.Should().Be(ErrorCode.InvalidArrayElementType);
    }

    [Fact]
    public void BindArrayCreationWithUnresolvedElementTypeShouldReportDiagnosticAndReturnNullResultType()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundExpression = TestUtils.BindExpression<BoundArrayCreationExpression>("new NonExistentType[5]", diagnosticBuilder);

        boundExpression.ResultType.Should().BeNull();

        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().ContainSingle();
        diagnostics.Single().ErrorCode.Should().Be(ErrorCode.TypeNotFound);
    }
}
