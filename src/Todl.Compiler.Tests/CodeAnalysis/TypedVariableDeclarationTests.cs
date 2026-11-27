using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class TypedVariableDeclarationTests
{
    [Theory]
    [InlineData("let x = 1;", null)]
    [InlineData("const x = 1;", null)]
    [InlineData("let uint = 1;", null)]
    [InlineData("let int x = 1;", "int")]
    [InlineData("const long x = 1L;", "long")]
    [InlineData("let double x = 1.5;", "double")]
    [InlineData("let Int64 x = 1L;", "Int64")]
    [InlineData("let System::Int64 x = 1L;", "System::Int64")]
    [InlineData("let int[] x = [1];", "int[]")]
    [InlineData("const string[] x = [\"a\"];", "string[]")]
    [InlineData("let Foo[][] x = 1;", "Foo[][]")]
    public void ParsingAnOptionalTypeExpression(string inputText, string expectedType)
    {
        var statement = TestUtils.ParseStatement<VariableDeclarationStatement>(inputText);

        if (expectedType is null)
        {
            statement.TypeExpression.Should().BeNull();
        }
        else
        {
            statement.TypeExpression.GetText().Should().Be(expectedType);
        }

        statement.IdentifierToken.Text.ToString().Should().Be(inputText.Contains("uint") ? "uint" : "x");
        statement.InitializerExpression.Should().NotBeNull();
    }

    [Fact]
    public void ModuleLevelDeclarationParsesTheTypeExpression()
    {
        var syntaxTree = TestUtils.ParseSyntaxTree("const long a = 0L; let System::Int32 b = 1; const c = 2; void Main() {}");

        syntaxTree.Members.OfType<VariableDeclarationMember>()
            .Select(m => m.VariableDeclarationStatement.TypeExpression?.GetText())
            .Should().Equal("long", "System::Int32", null);
    }

    [Theory]
    [InlineData("let int x = 1;", SpecialType.ClrInt32)]
    [InlineData("const long x = 1L;", SpecialType.ClrInt64)]
    [InlineData("const double x = 1.5;", SpecialType.ClrDouble)]
    [InlineData("let System::Int32 x = 1;", SpecialType.ClrInt32)]
    public void TypedLocalVariableUsesTheDeclaredType(string inputText, SpecialType expectedType)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var statement = TestUtils.BindStatement<BoundVariableDeclarationStatement>(inputText, diagnosticBuilder);

        diagnosticBuilder.Build().Should().BeEmpty();
        statement.Variable.Type.SpecialType.Should().Be(expectedType);
        statement.InitializerExpression.ResultType.SpecialType.Should().Be(expectedType);
    }

    [Fact]
    public void UntypedDeclarationInfersTheInitializerType()
    {
        var statement = TestUtils.BindStatement<BoundVariableDeclarationStatement>("let x = 1L;");

        statement.Variable.DeclaredType.Should().BeNull();
        statement.Variable.Type.SpecialType.Should().Be(SpecialType.ClrInt64);
    }

    [Fact]
    public void TypedArrayDeclarationBinds()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var statement = TestUtils.BindStatement<BoundVariableDeclarationStatement>("const string[] xs = [\"a\"];", diagnosticBuilder);

        diagnosticBuilder.Build().Should().BeEmpty();
        statement.Variable.Type.IsArray.Should().BeTrue();
        statement.Variable.Type.Name.Should().Be("System.String[]");
    }

    [Fact]
    public void ModuleLevelTypedDeclarationsBind()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var module = TestUtils.BindModule("const long a = 0L; let int b = 1; void Main() {}", diagnosticBuilder);

        diagnosticBuilder.Build().Should().BeEmpty();
        module.EntryPointType.BoundMembers.OfType<BoundVariableMember>()
            .Select(m => m.BoundVariableDeclarationStatement.Variable.Type.SpecialType)
            .Should().Equal(SpecialType.ClrInt64, SpecialType.ClrInt32);
    }

    [Theory]
    [InlineData("const long a = 0L;", SpecialType.ClrInt64)]
    [InlineData("const int a = 1 + 2;", SpecialType.ClrInt32)]
    [InlineData("const double a = 1.5;", SpecialType.ClrDouble)]
    public void ConstantFoldingOfTypedConstDeclaration(string inputText, SpecialType expectedType)
    {
        var rewriter = new ConstantFoldingBoundTreeRewriter(TestDefaults.ConstantValueFactory, new DiagnosticBag.Builder());
        var statement = TestUtils
            .BindStatement<BoundVariableDeclarationStatement>(inputText)
            .Accept(rewriter)
            .As<BoundVariableDeclarationStatement>();

        statement.Variable.Constant.Should().BeTrue();
        statement.Variable.Type.SpecialType.Should().Be(expectedType);
        statement.InitializerExpression.Should().BeOfType<BoundConstant>()
            .Which.ResultType.SpecialType.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("const long a = 0;", ErrorCode.TypeMismatch)]
    [InlineData("const int x = \"hi\";", ErrorCode.TypeMismatch)]
    [InlineData("const int x = 1L;", ErrorCode.TypeMismatch)]
    [InlineData("const int x = 1.5;", ErrorCode.TypeMismatch)]
    [InlineData("let int[] xs = 1;", ErrorCode.TypeMismatch)]
    [InlineData("let void v = 1;", ErrorCode.InvalidVariableType)]
    [InlineData("let Missing m = 1;", ErrorCode.TypeNotFound)]
    public void InvalidTypedDeclarationsAreReported(string inputText, ErrorCode expectedErrorCode)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindStatement<BoundVariableDeclarationStatement>(inputText, diagnosticBuilder);

        var diagnostic = diagnosticBuilder.Build().Should().ContainSingle().Subject;
        diagnostic.ErrorCode.Should().Be(expectedErrorCode);
        diagnostic.Level.Should().Be(DiagnosticLevel.Error);
    }

    [Theory]
    [InlineData("const long a = 0; void Main() {}", ErrorCode.TypeMismatch)]
    [InlineData("const string s = 1; void Main() {}", ErrorCode.TypeMismatch)]
    [InlineData("const void v = 1; void Main() {}", ErrorCode.InvalidVariableType)]
    [InlineData("const Missing m = 1; void Main() {}", ErrorCode.TypeNotFound)]
    public void InvalidModuleLevelTypedDeclarationsAreReported(string inputText, ErrorCode expectedErrorCode)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindModule(inputText, diagnosticBuilder);

        diagnosticBuilder.Build().Should().ContainSingle().Which.ErrorCode.Should().Be(expectedErrorCode);
    }
}
