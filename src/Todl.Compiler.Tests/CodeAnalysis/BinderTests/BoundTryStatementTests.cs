using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class BoundTryStatementTests
{
    private static Diagnostic[] BindStatementDiagnostics(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindStatement<BoundStatement>(inputText, diagnosticBuilder);
        return diagnosticBuilder.Build().ToArray();
    }

    private static Diagnostic[] BindMemberDiagnostics(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindMember<BoundFunctionMember>(inputText, diagnosticBuilder);
        return diagnosticBuilder.Build().ToArray();
    }

    private static void ShouldReportOnly(Diagnostic[] diagnostics, ErrorCode errorCode, int count = 1)
    {
        diagnostics.Should().HaveCount(count);
        diagnostics.Should().OnlyContain(d => d.ErrorCode == errorCode);
    }

    [Fact]
    public void TryStatementBindsBodyClausesAndFinallyBlock()
    {
        var tryStatement = TestUtils.BindStatement<BoundTryStatement>(
            "try { 1.ToString(); } except System::FormatException e { } except { } finally { 2.ToString(); 3.ToString(); }");

        tryStatement.Body.Statements.Should().HaveCount(1);
        tryStatement.ExceptClauses.Should().HaveCount(2);
        tryStatement.FinallyBlock.Statements.Should().HaveCount(2);

        var typedClause = tryStatement.ExceptClauses[0];
        typedClause.ExceptionType.Name.Should().Be("System.FormatException");
        typedClause.ExceptionVariable.Name.Should().Be("e");

        var generalClause = tryStatement.ExceptClauses[1];
        generalClause.ExceptionType.Should().BeNull();
        generalClause.ExceptionVariable.Should().BeNull();
    }

    [Fact]
    public void TryStatementWithoutFinallyHasNoFinallyBlock()
    {
        TestUtils.BindStatement<BoundTryStatement>("try { } except { }").FinallyBlock.Should().BeNull();
    }

    [Fact]
    public void ExceptionVariableIsTypedAsClauseTypeAndReadOnly()
    {
        var tryStatement = TestUtils.BindStatement<BoundTryStatement>(
            "try { } except System::InvalidOperationException e { e.Message; }");

        var variable = tryStatement.ExceptClauses[0].ExceptionVariable;
        variable.Should().BeOfType<ExceptionVariableSymbol>();
        variable.ReadOnly.Should().BeTrue();
        variable.Type.Should().Be(tryStatement.ExceptClauses[0].ExceptionType);
        variable.Type.Name.Should().Be("System.InvalidOperationException");
    }

    [Fact]
    public void ExceptionVariableIsDeclaredOnlyInTheExceptBlockScope()
    {
        var tryStatement = TestUtils.BindStatement<BoundTryStatement>(
            "try { } except System::Exception e { } finally { }");
        var clause = tryStatement.ExceptClauses[0];

        clause.Body.Scope.LookupVariable("e").Should().Be(clause.ExceptionVariable);
        tryStatement.Body.Scope.LookupVariable("e").Should().BeNull();
        tryStatement.FinallyBlock.Scope.LookupVariable("e").Should().BeNull();
    }

    [Fact]
    public void ExceptionVariableIsNotVisibleOutsideItsExceptBlock()
    {
        var diagnostics = BindStatementDiagnostics(
            "{ try { e; } except System::Exception e { e.Message; } finally { e; } e; }");

        ShouldReportOnly(diagnostics, ErrorCode.UndeclaredVariable, count: 3);
    }

    [Fact]
    public void ExceptionVariableIsReadOnly()
    {
        var diagnostics = BindStatementDiagnostics(
            "try { } except System::Exception e { e = new System::Exception(); }");

        var diagnostic = diagnostics.Should().ContainSingle().Subject;
        diagnostic.ErrorCode.Should().Be(ErrorCode.ReadOnlyVariable);
        diagnostic.Message.Should().Be("Variable e is read-only");
    }

    [Fact]
    public void ExceptBlockLocalsDoNotLeakToOtherClauses()
    {
        var diagnostics = BindStatementDiagnostics(
            "try { } except System::FormatException { const leaked = 1; } except System::Exception { leaked; }");

        ShouldReportOnly(diagnostics, ErrorCode.UndeclaredVariable);
    }

    [Theory]
    [InlineData("try { } except string { }")]
    [InlineData("try { } except int e { }")]
    [InlineData("try { } except System::Exception[] { }")]
    [InlineData("throw \"error\";")]
    [InlineData("throw 1;")]
    [InlineData("throw new System::Uri(\"http://localhost\");")]
    public void NonExceptionTypesAreRejected(string inputText)
    {
        ShouldReportOnly(BindStatementDiagnostics(inputText), ErrorCode.NotAnExceptionType);
    }

    [Fact]
    public void ThrowOperandCannotBeATypeName()
    {
        ShouldReportOnly(BindStatementDiagnostics("throw System::Exception;"), ErrorCode.NotAnExceptionType);
    }

    [Fact]
    public void ExceptClauseWithUnknownTypeReportsTypeNotFound()
    {
        ShouldReportOnly(BindStatementDiagnostics("try { } except System::NoSuchException { }"), ErrorCode.TypeNotFound);
    }

    [Fact]
    public void ThrowAcceptsExceptionTypesAndDerivedTypes()
    {
        BindStatementDiagnostics("throw new System::Exception();").Should().BeEmpty();
        BindStatementDiagnostics("throw new System::InvalidOperationException(\"x\");").Should().BeEmpty();

        var throwStatement = TestUtils.BindStatement<BoundThrowStatement>("throw new System::FormatException();");
        throwStatement.IsRethrow.Should().BeFalse();
        throwStatement.Expression.ResultType.Name.Should().Be("System.FormatException");
    }

    [Theory]
    [InlineData("try { } except System::Exception { } except System::FormatException { }")]
    [InlineData("try { } except System::Exception { } except System::Exception { }")]
    [InlineData("try { } except System::FormatException { } except System::Exception { } except System::OverflowException { }")]
    public void ExceptClauseIdenticalToOrDerivedFromEarlierClauseIsUnreachable(string inputText)
    {
        var diagnostic = BindStatementDiagnostics(inputText).Should().ContainSingle().Subject;
        diagnostic.ErrorCode.Should().Be(ErrorCode.UnreachableExceptClause);
    }

    [Theory]
    [InlineData("try { } except System::FormatException { } except System::Exception { }")]
    [InlineData("try { } except System::FormatException { } except System::OverflowException { } except { }")]
    public void ExceptClausesFromSpecificToGeneralAreAccepted(string inputText)
    {
        BindStatementDiagnostics(inputText).Should().BeEmpty();
    }

    [Fact]
    public void ExceptClauseOrderingDoesNotRelateOutOfScopeTrys()
    {
        BindStatementDiagnostics(
            "{ try { } except System::Exception { } try { } except System::FormatException { } }").Should().BeEmpty();
    }

    [Theory]
    [InlineData("try { } except { } except System::Exception { }", 1)]
    [InlineData("try { } except { } except { }", 1)]
    [InlineData("try { } except { } except { } except System::Exception { }", 2)]
    public void GeneralExceptClauseMustBeLastAndUnique(string inputText, int expectedCount)
    {
        ShouldReportOnly(BindStatementDiagnostics(inputText), ErrorCode.MisplacedGeneralExceptClause, expectedCount);
    }

    [Fact]
    public void RethrowOutsideExceptBlockIsRejected()
    {
        ShouldReportOnly(BindStatementDiagnostics("throw;"), ErrorCode.RethrowOutsideExceptBlock);
    }

    [Theory]
    [InlineData("try { throw; } except { }")]
    [InlineData("try { } finally { throw; }")]
    [InlineData("try { } except { } finally { throw; }")]
    public void RethrowInTryOrFinallyBlockIsRejected(string inputText)
    {
        ShouldReportOnly(BindStatementDiagnostics(inputText), ErrorCode.RethrowOutsideExceptBlock);
    }

    [Theory]
    [InlineData("try { } except { throw; }")]
    [InlineData("try { } except System::Exception e { throw; }")]
    [InlineData("try { } except { try { throw; } finally { } }")]
    [InlineData("try { } except { try { } except { throw; } }")]
    [InlineData("try { } except { { throw; } }")]
    [InlineData("try { } except { while true { throw; } }")]
    [InlineData("try { } except { try { } finally { try { } except { throw; } } }")]
    public void RethrowInsideExceptBlockIsAccepted(string inputText)
    {
        BindStatementDiagnostics(inputText).Should().BeEmpty();
    }

    [Theory]
    [InlineData("try { } except { try { } finally { throw; } }")]
    [InlineData("try { } except { try { } except { } finally { { throw; } } }")]
    public void RethrowInFinallyBlockNestedInExceptBlockIsRejected(string inputText)
    {
        ShouldReportOnly(BindStatementDiagnostics(inputText), ErrorCode.RethrowOutsideExceptBlock);
    }

    [Fact]
    public void ReturnInFinallyBlockIsRejected()
    {
        var diagnostics = BindMemberDiagnostics("int F() { try { return 1; } finally { return 2; } }");

        ShouldReportOnly(diagnostics, ErrorCode.JumpOutOfFinally);
    }

    [Theory]
    [InlineData("void F() { try { } finally { try { } except { return; } } }")]
    [InlineData("void F() { try { } finally { if true { return; } } }")]
    public void ReturnNestedInFinallyBlockIsRejected(string inputText)
    {
        ShouldReportOnly(BindMemberDiagnostics(inputText), ErrorCode.JumpOutOfFinally);
    }

    [Theory]
    [InlineData("int F() { try { return 1; } except { return 2; } finally { } }")]
    [InlineData("void F() { try { return; } except { return; } }")]
    public void ReturnInTryAndExceptBlocksIsAccepted(string inputText)
    {
        BindMemberDiagnostics(inputText).Should().BeEmpty();
    }

    [Theory]
    [InlineData("void F() { while true { try { } finally { break; } } }")]
    [InlineData("void F() { while true { try { } finally { continue; } } }")]
    [InlineData("void F() { while true: outer { while true { try { } finally { break outer; } } } }")]
    [InlineData("void F() { while true: outer { try { } finally { while true { break outer; } } } }")]
    [InlineData("void F() { while true { try { } finally { try { } finally { while true { } continue; } } } }")]
    public void JumpExitingFinallyBlockIsRejected(string inputText)
    {
        ShouldReportOnly(BindMemberDiagnostics(inputText), ErrorCode.JumpOutOfFinally);
    }

    [Theory]
    [InlineData("void F() { try { } finally { while true { break; } } }")]
    [InlineData("void F() { try { } finally { while true { continue; } } }")]
    [InlineData("void F() { while true { try { break; } finally { } } }")]
    [InlineData("void F() { while true { try { } except { continue; } finally { } } }")]
    [InlineData("void F() { while true { try { } finally { while true: inner { while true { break inner; } } } } }")]
    [InlineData("void F() { try { } finally { while true: a { try { } finally { while true { break; } } break a; } } }")]
    public void JumpWithinLoopInsideFinallyBlockIsAccepted(string inputText)
    {
        BindMemberDiagnostics(inputText).Should().BeEmpty();
    }

    [Fact]
    public void JumpFromNestedFinallyToLoopOutsideInnerFinallyButInsideOuterIsRejected()
    {
        var diagnostics = BindMemberDiagnostics(
            "void F() { try { } finally { while true { try { } finally { break; } } } }");

        ShouldReportOnly(diagnostics, ErrorCode.JumpOutOfFinally);
    }

    [Fact]
    public void FunctionLevelHandlersLowerToTryStatementWrappingBody()
    {
        var function = TestUtils.BindMember<BoundFunctionMember>(
            "int F() { const a = 1; return a; } except System::FormatException e { return 2; } finally { 1.ToString(); }");

        var tryStatement = function.Body.Statements.Should().ContainSingle().Subject.As<BoundTryStatement>();
        tryStatement.Body.Statements.Should().HaveCount(2);
        tryStatement.Body.Scope.Parent.Should().Be(function.FunctionScope);
        tryStatement.ExceptClauses.Should().ContainSingle();
        tryStatement.ExceptClauses[0].ExceptionVariable.Name.Should().Be("e");
        tryStatement.FinallyBlock.Statements.Should().HaveCount(1);
        function.Body.Scope.Should().Be(function.FunctionScope);
    }

    [Fact]
    public void VoidFunctionWithHandlersReturnsAfterTryStatement()
    {
        var function = TestUtils.BindMember<BoundFunctionMember>("void F() { 1.ToString(); } except { }");

        function.Body.Statements.Should().HaveCount(2);
        function.Body.Statements[0].Should().BeOfType<BoundTryStatement>();
        function.Body.Statements[1].Should().BeOfType<BoundReturnStatement>();
    }

    [Fact]
    public void VoidFunctionWithHandlersReturnsAfterTryStatementEvenIfBodyReturns()
    {
        var function = TestUtils.BindMember<BoundFunctionMember>("void F() { return; } finally { }");

        function.Body.Statements.Select(s => s.GetType()).Should().Equal(typeof(BoundTryStatement), typeof(BoundReturnStatement));
    }

    [Fact]
    public void NonVoidFunctionWithHandlersHasNoImplicitReturn()
    {
        var function = TestUtils.BindMember<BoundFunctionMember>("int F() { return 1; } except { return 2; }");

        function.Body.Statements.Should().ContainSingle().Which.Should().BeOfType<BoundTryStatement>();
    }

    [Fact]
    public void FunctionWithoutHandlersBodyIsNotWrapped()
    {
        var function = TestUtils.BindMember<BoundFunctionMember>("int F() { return 1; }");

        function.Body.Statements.Should().ContainSingle().Which.Should().BeOfType<BoundReturnStatement>();
    }

    [Fact]
    public void FunctionLevelHandlersSeeParametersButNotBodyLocals()
    {
        var diagnostics = BindMemberDiagnostics(
            "void F(int p) { const local = 1; } except { p; local; } finally { p; local; }");

        ShouldReportOnly(diagnostics, ErrorCode.UndeclaredVariable, count: 2);
        diagnostics.Select(d => d.TextLocation.GetText()).Should().OnlyContain(t => t == "local");
    }

    [Fact]
    public void FunctionLevelHandlersCanUseParameters()
    {
        BindMemberDiagnostics(
            "void F(int p) { } except System::Exception e { p.ToString(); e.Message.ToString(); } finally { p.ToString(); }")
            .Should().BeEmpty();
    }

    [Fact]
    public void FunctionLevelRethrowAndFinallyRulesApply()
    {
        BindMemberDiagnostics("void F() { } except { throw; }").Should().BeEmpty();
        ShouldReportOnly(BindMemberDiagnostics("void F() { throw; } except { }"), ErrorCode.RethrowOutsideExceptBlock);
        ShouldReportOnly(BindMemberDiagnostics("int F() { return 1; } finally { return 2; }"), ErrorCode.JumpOutOfFinally);
        ShouldReportOnly(BindMemberDiagnostics("void F() { } except System::Exception { } except System::FormatException { }"), ErrorCode.UnreachableExceptClause);
    }

    [Fact]
    public void FunctionLevelExceptionVariableIsReadOnly()
    {
        var diagnostics = BindMemberDiagnostics(
            "void F() { } except System::Exception e { e = new System::Exception(); }");

        ShouldReportOnly(diagnostics, ErrorCode.ReadOnlyVariable);
    }

    [Fact]
    public void ConstantFoldingAndStringLoweringReachInsideTryStatements()
    {
        var tryStatement = TestUtils
            .BindStatement<BoundStatement>(
                "{ const x = \"a\"; try { let t = x + \"1\"; } except System::Exception e { let c = x + \"2\"; } finally { let f = x + \"3\"; throw new System::Exception(x + \"4\"); } }")
            .Accept(new ConstantFoldingBoundTreeRewriter(TestDefaults.ConstantValueFactory, new DiagnosticBag.Builder()))
            .Accept(new StringConcatenationLoweringBoundTreeRewriter(TestDefaults.ConstantValueFactory))
            .As<BoundBlockStatement>()
            .Statements[1]
            .As<BoundTryStatement>();

        BoundExpression Initializer(BoundStatement statement)
            => statement.As<BoundVariableDeclarationStatement>().InitializerExpression;

        Initializer(tryStatement.Body.Statements[0]).Should().BeOfType<BoundConstant>().Which.Value.Should().Be("a1");
        Initializer(tryStatement.ExceptClauses[0].Body.Statements[0]).Should().BeOfType<BoundConstant>().Which.Value.Should().Be("a2");
        Initializer(tryStatement.FinallyBlock.Statements[0]).Should().BeOfType<BoundConstant>().Which.Value.Should().Be("a3");

        var thrown = tryStatement.FinallyBlock.Statements[1].As<BoundThrowStatement>().Expression
            .As<BoundObjectCreationExpression>();
        thrown.BoundArguments.Single().Should().BeOfType<BoundConstant>().Which.Value.Should().Be("a4");
    }

    [Fact]
    public void ExceptionVariableCannotReuseParameterOrEnclosingLocalName()
    {
        ShouldReportOnly(
            BindMemberDiagnostics("void F(int e) { try { } except System::Exception e { } }"),
            ErrorCode.DuplicateVariableDeclaration);
        ShouldReportOnly(
            BindMemberDiagnostics("void F() { let e = 1; try { } except System::Exception e { } }"),
            ErrorCode.DuplicateVariableDeclaration);
        ShouldReportOnly(
            BindMemberDiagnostics("void F(int e) { } except System::Exception e { }"),
            ErrorCode.DuplicateVariableDeclaration);
    }

    [Fact]
    public void ExceptionVariablesInSiblingClausesMayShareAName()
    {
        BindMemberDiagnostics(
            "void F() { try { } except System::FormatException e { } except System::Exception e { } }")
            .Should().BeEmpty();
    }

    [Fact]
    public void HandlerLocalsMayReuseBodyLocalNames()
    {
        BindMemberDiagnostics("void F() { let a = 1; } except { let a = 2; }").Should().BeEmpty();
    }
}
