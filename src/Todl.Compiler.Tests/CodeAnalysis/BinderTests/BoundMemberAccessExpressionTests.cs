using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundMemberAccessExpressionTests
{
    [Fact]
    public void TestBoundClrPropertyAccessExpressionInstance()
    {
        var boundClrPropertyAccessExpression = TestUtils.BindExpression<BoundClrPropertyAccessExpression>("\"abc\".Length");

        boundClrPropertyAccessExpression.GetDiagnostics().Should().BeEmpty();
        boundClrPropertyAccessExpression.MemberName.Should().Be("Length");
        boundClrPropertyAccessExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrInt32);
        boundClrPropertyAccessExpression.IsStatic.Should().Be(false);
        boundClrPropertyAccessExpression.ReadOnly.Should().Be(true);
        boundClrPropertyAccessExpression.IsPublic.Should().Be(true);
    }

    [Fact]
    public void TestBoundClrFieldAccessExpressionStatic()
    {
        var boundClrFieldAccessExpression = TestUtils.BindExpression<BoundClrFieldAccessExpression>("System.Int32.MaxValue");

        boundClrFieldAccessExpression.GetDiagnostics().Should().BeEmpty();
        boundClrFieldAccessExpression.MemberName.Should().Be("MaxValue");
        boundClrFieldAccessExpression.ResultType.SpecialType.Should().Be(SpecialType.ClrInt32);
        boundClrFieldAccessExpression.IsStatic.Should().Be(true);
        boundClrFieldAccessExpression.ReadOnly.Should().Be(true);
        boundClrFieldAccessExpression.IsPublic.Should().Be(true);
    }

    [Fact]
    public void TestBoundInvalidMemberAccessExpression()
    {
        var boundExpression = TestUtils.BindExpression<BoundMemberAccessExpression>("System.Int32.Maxvalue");

        var diagnostic = boundExpression.GetDiagnostics().First();
        diagnostic.Level.Should().Be(DiagnosticLevel.Error);
        diagnostic.Message.Should().Be("Member 'Maxvalue' does not exist in type 'System.Int32'");
    }
}
