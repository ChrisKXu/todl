using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundArrayLiteralExpressionTests
{
    [Fact]
    public void TestBoundArrayLiteralExpressionBasic()
    {
        var boundArrayLiteralExpression = TestUtils.BindExpression<BoundArrayLiteralExpression>("[1, 2, 3]");

        boundArrayLiteralExpression.BoundElements.Should().HaveCount(3);
        ((ClrTypeSymbol)boundArrayLiteralExpression.ResultType).ClrType.FullName.Should().Be("System.Int32[]");
    }

    [Fact]
    public void TestBoundNestedArrayLiteralExpression()
    {
        var boundArrayLiteralExpression = TestUtils.BindExpression<BoundArrayLiteralExpression>("[[1, 2], [3, 4, 5]]");

        ((ClrTypeSymbol)boundArrayLiteralExpression.ResultType).ClrType.FullName.Should().Be("System.Int32[][]");
        boundArrayLiteralExpression.BoundElements.Should().AllBeOfType<BoundArrayLiteralExpression>();
    }

    [Fact]
    public void BindArrayLiteralWithMismatchedElementTypesShouldReportDiagnosticAndReturnNullResultType()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundExpression = TestUtils.BindExpression<BoundArrayLiteralExpression>("[1, \"two\"]", diagnosticBuilder);

        boundExpression.ResultType.Should().BeNull();

        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().ContainSingle();
        diagnostics.Single().Level.Should().Be(DiagnosticLevel.Error);
        diagnostics.Single().ErrorCode.Should().Be(ErrorCode.ArrayElementTypeMismatch);
    }
}
