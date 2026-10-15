using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundVariableDeclarationStatementTests
{
    [Theory]
    [InlineData("void f(int a) { let a = 1; }")]
    [InlineData("void f() { let a = 1; let a = 2; }")]
    [InlineData("void f() { let a = 1; const a = \"s\"; }")]
    [InlineData("void f() { let a = 1; { let a = 2; } }")]
    [InlineData("void f(int a) { { let a = 2; } }")]
    [InlineData("void f() { let a = 1; if true { let a = 2; } }")]
    [InlineData("void f() { let a = 1; while true { if true { let a = 2; } } }")]
    public void RedeclaringAVisibleLocalShouldReportError(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindModule(inputText, diagnosticBuilder).Should().NotBeNull();

        var diagnostic = diagnosticBuilder.Build().Should().ContainSingle().Subject;
        diagnostic.ErrorCode.Should().Be(ErrorCode.DuplicateVariableDeclaration);
        diagnostic.Level.Should().Be(DiagnosticLevel.Error);
        diagnostic.Message.Should().Contain("'a'");
    }

    [Theory]
    [InlineData("void f() { { let a = 1; } { let a = 2; } }")]
    [InlineData("void f(bool c) { if c { let a = 1; } else { let a = 2; } }")]
    [InlineData("void f(bool c) { while c { let a = 1; } while c { let a = 2; } }")]
    [InlineData("void f() { { let a = 1; } let a = 2; }")]
    [InlineData("void f() { if true { let a = 1; } const a = 2; }")]
    [InlineData("const a = 1; void f() { let a = 2; }")]
    [InlineData("void f(int a) { } void g() { let a = 1; }")]
    [InlineData("void g() { let b = 1; } void f(int b) { let a = b; }")]
    [InlineData("void a() { } void f() { let a = 1; }")]
    public void NonOverlappingOrOutOfFunctionDeclarationsShouldBeAllowed(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindModule(inputText, diagnosticBuilder).Should().NotBeNull();
        diagnosticBuilder.Build().Should().BeEmpty();
    }

    [Fact]
    public void RedeclarationShouldBeReportedAtTheIdentifierOfTheSecondDeclaration()
    {
        var inputText = "void f() { let a = 1; let a = 2; }";
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindModule(inputText, diagnosticBuilder);

        var location = diagnosticBuilder.Build().Single().TextLocation;
        location.TextSpan.Start.Should().Be(inputText.LastIndexOf('a'));
        location.GetText().Should().Be("a");
    }

    [Fact]
    public void RedeclaredShadowingVariableShouldNotCascadeErrors()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindModule("void f() { let a = 1; { let a = \"s\"; a.ToString(); } }", diagnosticBuilder);

        diagnosticBuilder.Build().Should().ContainSingle()
            .Which.ErrorCode.Should().Be(ErrorCode.DuplicateVariableDeclaration);
    }
}
