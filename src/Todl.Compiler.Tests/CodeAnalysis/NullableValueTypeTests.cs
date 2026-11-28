using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeAnalysis;

public sealed class NullableValueTypeTests
{
    private const string HelperImport = "import { NullableTestHelper, NullableTestHolder } from Todl::Compiler::Tests;\n";

    [Theory]
    [InlineData("let int? x = null;", "int?")]
    [InlineData("const DateTime? d = null;", "DateTime?")]
    [InlineData("const System::DateTime? d = null;", "System::DateTime?")]
    [InlineData("let int?[] x = null;", "int?[]")]
    [InlineData("let System::Int32?[][] x = null;", "System::Int32?[][]")]
    [InlineData("let long? x = 1L;", "long?")]
    public void ParsingNullableTypeExpressionsInDeclarations(string inputText, string expectedType)
    {
        var statement = TestUtils.ParseStatement<VariableDeclarationStatement>(inputText);

        statement.TypeExpression.GetText().Should().Be(expectedType);
        statement.TypeExpression.IsNullableType.Should().BeTrue();
        statement.IdentifierToken.Text.ToString().Should().NotBeEmpty();
    }

    [Fact]
    public void QuestionMarkPrecedesTheArrayRankSpecifiers()
    {
        var statement = TestUtils.ParseStatement<VariableDeclarationStatement>("let int?[][] x = null;");

        statement.TypeExpression.BaseTypeExpression.GetText().Should().Be("int");
        statement.TypeExpression.QuestionToken.Should().NotBeNull();
        statement.TypeExpression.ArrayRankSpecifiers.Should().HaveCount(2);
        statement.TypeExpression.IsArrayType.Should().BeTrue();
    }

    [Fact]
    public void NullLiteralParsesAsAnExpression()
    {
        var statement = TestUtils.ParseStatement<VariableDeclarationStatement>("const int? x = null;");

        statement.InitializerExpression.Should().BeOfType<NullLiteralExpression>()
            .Which.NullKeywordToken.Kind.Should().Be(SyntaxKind.NullKeywordToken);
    }

    [Fact]
    public void NullableTypesParseInFunctionSignaturesAndNewArrayExpressions()
    {
        var function = TestUtils.ParseMember<FunctionDeclarationMember>("int? F(long? a, int?[] b) { return null; }");

        function.ReturnType.IsNullableType.Should().BeTrue();
        function.Parameters.Items.Select(p => p.ParameterType.GetText()).Should().Equal("long?", "int?[]");

        var newArray = TestUtils.ParseExpression<NewArrayExpression>("new int?[3]");
        newArray.QuestionToken.Should().NotBeNull();
        newArray.GetText().Should().Be("new int?[3]");
    }

    [Theory]
    [InlineData("let int?? x = null;")]
    [InlineData("let int??? x = null;")]
    public void NestedNullableTypeIsReportedByTheParser(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.ParseStatement<VariableDeclarationStatement>(inputText, diagnosticBuilder);

        diagnosticBuilder.Build().Should().OnlyContain(d => d.ErrorCode == ErrorCode.InvalidNullableType);
    }

    [Fact]
    public void QuestionMarkAfterAnArrayTypeIsAParseError()
    {
        var diagnostics = CompileDiagnostics("void Main() { let int[]? x = null; }");

        diagnostics.Should().Contain(d => d.ErrorCode == ErrorCode.InvalidNullableType && d.Level == DiagnosticLevel.Error);
    }

    [Theory]
    [InlineData("void Main() { const int? a = 1; const int b = a + 1; }", ErrorCode.UnsupportedOperator)]
    [InlineData("void Main() { const bool? a = true; if a { } }", ErrorCode.TypeMismatch)]
    [InlineData("void Main() { const int? a = 1; const int b = a; }", ErrorCode.TypeMismatch)]
    [InlineData("void Main() { let x = null; }", ErrorCode.UntypedNullLiteral)]
    [InlineData("void? Main() { }", ErrorCode.InvalidNullableType)]
    [InlineData("void Main() { const int?? a = 1; }", ErrorCode.InvalidNullableType)]
    [InlineData("void Main(string? a) { }", ErrorCode.InvalidNullableType)]
    public void CompilationReportsNullableErrorsInsteadOfThrowing(string inputText, ErrorCode expectedErrorCode)
    {
        CompileDiagnostics(inputText).Should().Contain(d => d.ErrorCode == expectedErrorCode && d.Level == DiagnosticLevel.Error);
    }

    private static System.Collections.Generic.IEnumerable<Diagnostic> CompileDiagnostics(string inputText)
    {
        var compilation = new Todl.Compiler.CodeGeneration.Compilation(
            "test",
            new System.Version(1, 0),
            [Todl.Compiler.CodeAnalysis.Text.SourceText.FromString(inputText)],
            TestDefaults.DefaultClrTypeCache);

        return compilation.GetDiagnostics().ToArray();
    }

    [Fact]
    public void NullableTypeIsTheSameSymbolAsTheClrNullableFromMetadata()
    {
        var statement = TestUtils.BindStatement<BoundVariableDeclarationStatement>("const int? a = 1;");
        var helperType = TestDefaults.DefaultClrTypeCache.Resolve("Todl.Compiler.Tests.NullableTestHelper");
        var clrNullable = TestDefaults.DefaultClrTypeCache.Resolve(helperType.ClrType.GetMethod("Echo").ReturnType);

        statement.Variable.Type.Should().BeSameAs(clrNullable);
        statement.Variable.Type.IsNullableValueType.Should().BeTrue();
        statement.Variable.Type.UnderlyingNullableType.SpecialType.Should().Be(SpecialType.ClrInt32);
        statement.Variable.Type.IsReferenceType.Should().BeFalse();
        statement.Variable.Type.Name.Should().Be("System.Int32?");
    }

    [Fact]
    public void NonNullableTypesAreNotNullableValueTypes()
    {
        var statement = TestUtils.BindStatement<BoundVariableDeclarationStatement>("const int a = 1;");

        statement.Variable.Type.IsNullableValueType.Should().BeFalse();
        statement.Variable.Type.UnderlyingNullableType.Should().BeNull();
    }

    [Theory]
    [InlineData("const int? a = 1;", ConversionKind.ImplicitNullable)]
    [InlineData("let long? a = 0L;", ConversionKind.ImplicitNullable)]
    [InlineData("let bool? a = true;", ConversionKind.ImplicitNullable)]
    [InlineData("const System::DateTime? a = new System::DateTime(1);", ConversionKind.ImplicitNullable)]
    [InlineData("const int? a = null;", ConversionKind.NullLiteral)]
    [InlineData("const System::DateTime? a = null;", ConversionKind.NullLiteral)]
    [InlineData("const int? a = (null);", ConversionKind.NullLiteral)]
    public void TypedNullableDeclarationsConvertTheInitializer(string inputText, ConversionKind expectedKind)
    {
        var statement = TestUtils.BindStatement<BoundVariableDeclarationStatement>(inputText);

        statement.Variable.Type.IsNullableValueType.Should().BeTrue();
        var conversion = statement.InitializerExpression.Should().BeOfType<BoundConversionExpression>().Subject;
        conversion.ConversionKind.Should().Be(expectedKind);
        conversion.ResultType.Should().BeSameAs(statement.Variable.Type);
        conversion.Constant.Should().Be(conversion.Operand.Constant || expectedKind == ConversionKind.NullLiteral);
    }

    [Fact]
    public void NullableValueIsKeptWithoutConversionWhenTypesAreIdentical()
    {
        var block = TestUtils.BindStatement<BoundBlockStatement>("{ const int? a = 1; const int? b = a; }");

        block.Statements[1].As<BoundVariableDeclarationStatement>().InitializerExpression
            .Should().BeOfType<BoundVariableExpression>();
    }

    [Fact]
    public void NullableArraysBindWithNullableElementType()
    {
        var statement = TestUtils.BindStatement<BoundVariableDeclarationStatement>("let int?[] xs = new int?[3];");

        statement.Variable.Type.IsArray.Should().BeTrue();
        statement.Variable.Type.Name.Should().StartWith("System.Nullable`1[[System.Int32").And.EndWith("[]");
    }

    [Theory]
    [InlineData("{ let int? x = 1; x = 2; x = null; }")]
    [InlineData("{ let int?[] xs = new int?[2]; xs[0] = 1; xs[1] = null; }")]
    [InlineData("{ let int? x = 1; let int? y = 2; x = y; }")]
    [InlineData("{ let int? x = 1; x = (null); }")]
    [InlineData("{ let x = new Todl::Compiler::Tests::NullableTestHolder(); x.Field = 1; x.Field = null; x.Property = 1.5; }")]
    public void AssignmentsToNullableTargetsBind(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindStatement<BoundBlockStatement>(inputText, diagnosticBuilder);

        diagnosticBuilder.Build().Should().BeEmpty();
    }

    [Fact]
    public void AssignedValueIsConvertedToTheTargetType()
    {
        var block = TestUtils.BindStatement<BoundBlockStatement>("{ let int? x = 1; x = 2; x = null; }");

        block.Statements[1].As<BoundExpressionStatement>().Expression.As<BoundAssignmentExpression>().Right
            .As<BoundConversionExpression>().ConversionKind.Should().Be(ConversionKind.ImplicitNullable);
        block.Statements[2].As<BoundExpressionStatement>().Expression.As<BoundAssignmentExpression>().Right
            .As<BoundConversionExpression>().ConversionKind.Should().Be(ConversionKind.NullLiteral);
    }

    [Theory]
    [InlineData("int? F() { return 1; }", ConversionKind.ImplicitNullable)]
    [InlineData("int? F() { return null; }", ConversionKind.NullLiteral)]
    [InlineData("int? F() { return (null); }", ConversionKind.NullLiteral)]
    public void ReturnValuesAreConvertedToTheNullableReturnType(string inputText, ConversionKind expectedKind)
    {
        var function = TestUtils.BindMember<BoundFunctionMember>(inputText);

        function.ReturnType.IsNullableValueType.Should().BeTrue();
        function.Body.Statements.OfType<BoundReturnStatement>().Single().BoundReturnValueExpression
            .As<BoundConversionExpression>().ConversionKind.Should().Be(expectedKind);
    }

    [Fact]
    public void NullableParametersAndReturnTypesResolveToNullableSymbols()
    {
        var function = TestUtils.BindMember<BoundFunctionMember>("int? F(long? a, bool?[] b) { return a; }", new DiagnosticBag.Builder());

        function.FunctionSymbol.Parameters[0].Type.IsNullableValueType.Should().BeTrue();
        function.FunctionSymbol.Parameters[1].Type.IsArray.Should().BeTrue();
    }

    [Theory]
    [InlineData("int? Id(int? v) { return v; } void Main() { const int? a = Id(1); const int? b = Id(null); }")]
    [InlineData("int? Id(int? v) { return v; } void Main() { const int? a = Id(v: 1); const int? b = Id(v: null); }")]
    [InlineData("string Both(int a, int? b) { return \"x\"; } void Main() { Both(1, 2); Both(1, null); Both(a: 1, b: null); }")]
    public void ArgumentsToNullableParametersBind(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindModule(inputText, diagnosticBuilder);

        diagnosticBuilder.Build().Should().BeEmpty();
    }

    [Fact]
    public void TodlOverloadResolutionPrefersIdentityOverWrapping()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var module = TestUtils.BindModule(
            "int F(int a) { return 1; } int F(int? a) { return 2; } void Main() { F(1); F(null); const int? n = 1; F(n); }",
            diagnosticBuilder);

        diagnosticBuilder.Build().Should().BeEmpty();

        var main = module.EntryPointType.BoundMembers.OfType<BoundFunctionMember>().Single(f => f.FunctionSymbol.Name == "Main");
        var calls = main.Body.Statements
            .OfType<BoundExpressionStatement>()
            .Select(s => s.Expression.As<BoundTodlInvocationExpression>())
            .ToArray();

        calls[0].FunctionSymbol.Parameters[0].Type.IsNullableValueType.Should().BeFalse();
        calls[1].FunctionSymbol.Parameters[0].Type.IsNullableValueType.Should().BeTrue();
        calls[2].FunctionSymbol.Parameters[0].Type.IsNullableValueType.Should().BeTrue();
        calls[2].BoundArguments["a"].Should().BeOfType<BoundConversionExpression>();
    }

    [Fact]
    public void ClrOverloadResolutionPrefersIdentityOverWrapping()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var module = TestUtils.BindModule(
            HelperImport + "void Main() { NullableTestHelper.Pick(1); NullableTestHelper.Pick(null); }",
            diagnosticBuilder);

        diagnosticBuilder.Build().Should().BeEmpty();

        var calls = module.EntryPointType.BoundMembers.OfType<BoundFunctionMember>().Single()
            .Body.Statements.OfType<BoundExpressionStatement>()
            .Select(s => s.Expression.As<BoundClrInvocationExpression>())
            .ToArray();

        calls[0].MethodInfo.GetParameters()[0].ParameterType.IsValueType.Should().BeTrue();
        calls[0].MethodInfo.GetParameters()[0].ParameterType.IsGenericType.Should().BeFalse();
        calls[1].MethodInfo.GetParameters()[0].ParameterType.IsGenericType.Should().BeTrue();
    }

    [Theory]
    [InlineData("string G(int? a) { return \"a\"; } string G(long? a) { return \"b\"; } void Main() { G(null); }")]
    [InlineData("string G(int? a) { return \"a\"; } string G(long? a) { return \"b\"; } void Main() { G(a: null); }")]
    [InlineData("import { NullableTestHelper } from Todl::Compiler::Tests;\nvoid Main() { NullableTestHelper.Both(null); }")]
    [InlineData("import { NullableTestHelper } from Todl::Compiler::Tests;\nvoid Main() { NullableTestHelper.Both(value: null); }")]
    public void NullMatchingMultipleNullableOverloadsIsAmbiguous(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindModule(inputText, diagnosticBuilder);

        diagnosticBuilder.Build().Should().Contain(d => d.ErrorCode == ErrorCode.AmbiguousCall && d.Level == DiagnosticLevel.Error);
    }

    [Fact]
    public void SeveralNonExactCandidatesAreAmbiguousEvenWhenOneDominates()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindModule(
            "int F(int a, int? b) { return 1; } int F(int? a, int? b) { return 2; } void Main() { F(1, 1); }",
            diagnosticBuilder);

        diagnosticBuilder.Build().Should().Contain(d => d.ErrorCode == ErrorCode.AmbiguousCall);
    }

    [Fact]
    public void ClrConstructorsAcceptNullableArguments()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindModule(
            HelperImport + "void Main() { const a = new NullableTestHolder(1); const b = new NullableTestHolder(null); const c = new NullableTestHolder(); }",
            diagnosticBuilder);

        diagnosticBuilder.Build().Should().BeEmpty();
    }

    [Fact]
    public void NullableMembersAreExposedAsClrMembers()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var block = TestUtils.BindStatement<BoundBlockStatement>(
            "{ const int? a = 1; const bool h = a.HasValue; const int v = a.Value; const int d = a.GetValueOrDefault(); const int e = a.GetValueOrDefault(5); }",
            diagnosticBuilder);

        diagnosticBuilder.Build().Should().BeEmpty();
        block.Statements.Should().HaveCount(5);
    }

    [Theory]
    [InlineData("const int? a = 1; const int b = 2; a + b;")]
    [InlineData("const int? a = 1; const int b = 2; b + a;")]
    [InlineData("const int? a = 1; a + a;")]
    [InlineData("const int? a = 1; const int b = 2; a - b;")]
    [InlineData("const int? a = 1; a * a;")]
    [InlineData("const int? a = 1; a / a;")]
    [InlineData("const int? a = 1; const int b = 2; a < b;")]
    [InlineData("const int? a = 1; a <= a;")]
    [InlineData("const int? a = 1; a > a;")]
    [InlineData("const int? a = 1; a >= a;")]
    [InlineData("const int? a = 1; a == a;")]
    [InlineData("const int? a = 1; const int b = 2; a != b;")]
    [InlineData("const int? a = 1; -a;")]
    [InlineData("const int? a = 1; +a;")]
    [InlineData("const int? a = 1; ~a;")]
    [InlineData("const bool? b = true; !b;")]
    [InlineData("const bool? b = true; b && true;")]
    [InlineData("const bool? b = true; true || b;")]
    [InlineData("const bool? b = true; b && b;")]
    [InlineData("let int? a = 1; a += 1;")]
    [InlineData("let int? a = 1; a -= a;")]
    [InlineData("let int? a = 1; let int b = 2; b *= a;")]
    [InlineData("let int? a = 1; a /= 2;")]
    [InlineData("const int? a = 1; \"s\" + a;")]
    public void OperatorsAreNeverLiftedToNullableTypes(string statements)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindStatement<BoundBlockStatement>("{ " + statements + " }", diagnosticBuilder);

        var diagnostic = diagnosticBuilder.Build().Should().ContainSingle().Subject;
        diagnostic.ErrorCode.Should().Be(ErrorCode.UnsupportedOperator);
        diagnostic.Level.Should().Be(DiagnosticLevel.Error);
    }

    [Theory]
    [InlineData("const bool? b = true; if b { }")]
    [InlineData("const bool? b = true; unless b { }")]
    [InlineData("const bool? b = true; if true { } else if b { }")]
    [InlineData("const bool? b = true; unless true { } else unless b { }")]
    [InlineData("const bool? b = true; while b { }")]
    [InlineData("const bool? b = true; until b { }")]
    public void NullableBooleanIsNeverAValidCondition(string statements)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindStatement<BoundBlockStatement>("{ " + statements + " }", diagnosticBuilder);

        var diagnostic = diagnosticBuilder.Build().Should().ContainSingle().Subject;
        diagnostic.ErrorCode.Should().Be(ErrorCode.TypeMismatch);
        diagnostic.Message.Should().Contain("bool");
    }

    [Theory]
    [InlineData("const void? v = null;", ErrorCode.InvalidNullableType)]
    [InlineData("const string? s = null;", ErrorCode.InvalidNullableType)]
    [InlineData("const object? o = null;", ErrorCode.InvalidNullableType)]
    [InlineData("const System::Exception? e = null;", ErrorCode.InvalidNullableType)]
    [InlineData("let x = null;", ErrorCode.UntypedNullLiteral)]
    [InlineData("const int x = null;", ErrorCode.TypeMismatch)]
    [InlineData("const string s = null;", ErrorCode.TypeMismatch)]
    [InlineData("const int[] s = null;", ErrorCode.TypeMismatch)]
    [InlineData("const long? x = 0;", ErrorCode.TypeMismatch)]
    [InlineData("const int? x = 1L;", ErrorCode.TypeMismatch)]
    [InlineData("const int? x = true;", ErrorCode.TypeMismatch)]
    [InlineData("const long? x = 1.5;", ErrorCode.TypeMismatch)]
    [InlineData("const int x = 1; const int? y = null; const int z = y;", ErrorCode.TypeMismatch)]
    [InlineData("const int? y = null; const int? z = \"s\";", ErrorCode.TypeMismatch)]
    [InlineData("const int? y = null; y == null;", ErrorCode.UntypedNullLiteral)]
    [InlineData("const int? y = null; null != y;", ErrorCode.UntypedNullLiteral)]
    [InlineData("null;", ErrorCode.UntypedNullLiteral)]
    [InlineData("const bool b = null == null;", ErrorCode.UntypedNullLiteral)]
    public void InvalidNullableUsesAreReported(string statements, ErrorCode expectedErrorCode)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindStatement<BoundBlockStatement>("{ " + statements + " }", diagnosticBuilder);

        var diagnostics = diagnosticBuilder.Build();
        diagnostics.Should().NotBeEmpty();
        diagnostics.Should().OnlyContain(d => d.Level == DiagnosticLevel.Error);
        diagnostics.Should().Contain(d => d.ErrorCode == expectedErrorCode);
    }

    [Theory]
    [InlineData("let int? x = 1; let int y = 2; x = y; y = x;", ErrorCode.TypeMismatch)]
    [InlineData("let int x = 1; x = null;", ErrorCode.TypeMismatch)]
    [InlineData("let string s = \"\"; s = null;", ErrorCode.TypeMismatch)]
    [InlineData("let long? x = 1L; x = 1;", ErrorCode.TypeMismatch)]
    [InlineData("let int?[] xs = new int?[1]; xs[0] = 1L;", ErrorCode.TypeMismatch)]
    [InlineData("let int[] xs = new int[1]; xs[0] = null;", ErrorCode.TypeMismatch)]
    [InlineData("let int? x = 1; x += null;", ErrorCode.UntypedNullLiteral)]
    public void InvalidAssignmentsToNullableTargetsAreReported(string statements, ErrorCode expectedErrorCode)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindStatement<BoundBlockStatement>("{ " + statements + " }", diagnosticBuilder);

        diagnosticBuilder.Build().Should().Contain(d => d.ErrorCode == expectedErrorCode && d.Level == DiagnosticLevel.Error);
    }

    [Theory]
    [InlineData("int F() { return null; }", ErrorCode.TypeMismatch)]
    [InlineData("string F() { return null; }", ErrorCode.TypeMismatch)]
    [InlineData("void F() { return null; }", ErrorCode.TypeMismatch)]
    [InlineData("long? F() { return 1; }", ErrorCode.TypeMismatch)]
    [InlineData("int? F() { return 1L; }", ErrorCode.TypeMismatch)]
    [InlineData("int F(int? a) { return a; }", ErrorCode.TypeMismatch)]
    [InlineData("int? F(int a) { return a + a; } int G(int? a) { return a.Value; } int? H(int? a, int b) { return a + b; }", ErrorCode.UnsupportedOperator)]
    [InlineData("void? F() { }", ErrorCode.InvalidNullableType)]
    [InlineData("string? F() { return \"\"; }", ErrorCode.InvalidNullableType)]
    [InlineData("void F(string? s) { }", ErrorCode.InvalidNullableType)]
    [InlineData("void F(void? s) { }", ErrorCode.InvalidNullableType)]
    public void InvalidNullableFunctionsAreReported(string inputText, ErrorCode expectedErrorCode)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindModule(inputText + " void Main() { }", diagnosticBuilder);

        diagnosticBuilder.Build().Should().Contain(d => d.ErrorCode == expectedErrorCode && d.Level == DiagnosticLevel.Error);
    }

    [Theory]
    [InlineData("int F(int a) { return a; } void Main() { F(null); }", ErrorCode.NoMatchingCandidate)]
    [InlineData("int F(string a) { return 1; } void Main() { F(null); }", ErrorCode.NoMatchingCandidate)]
    [InlineData("int? F(long? a) { return null; } void Main() { F(1); }", ErrorCode.NoMatchingCandidate)]
    [InlineData("int? F(long? a) { return null; } void Main() { const int? x = 1; F(x); }", ErrorCode.NoMatchingCandidate)]
    [InlineData("int? F(int? a) { return a; } void Main() { F(1L); }", ErrorCode.NoMatchingCandidate)]
    [InlineData("int? F(int? a) { return a; } void Main() { F(a: 1L); }", ErrorCode.NoMatchingCandidate)]
    [InlineData("int? F(int? a) { return a; } void Main() { F(a: 1, b: null); }", ErrorCode.NoMatchingCandidate)]
    [InlineData("import { NullableTestHelper } from Todl::Compiler::Tests;\nvoid Main() { NullableTestHelper.Only(1); }", ErrorCode.NoMatchingCandidate)]
    [InlineData("import { NullableTestHelper } from Todl::Compiler::Tests;\nvoid Main() { NullableTestHelper.Pick(\"s\"); }", ErrorCode.NoMatchingCandidate)]
    [InlineData("import { NullableTestHelper } from Todl::Compiler::Tests;\nvoid Main() { System::Math.Abs(null); }", ErrorCode.NoMatchingCandidate)]
    [InlineData("import { NullableTestHolder } from Todl::Compiler::Tests;\nvoid Main() { new NullableTestHolder(1L); }", ErrorCode.NoMatchingCandidate)]
    [InlineData("import { NullableTestHolder } from Todl::Compiler::Tests;\nvoid Main() { new System::Text::StringBuilder(null, null, null); }", ErrorCode.NoMatchingCandidate)]
    public void InvalidNullableArgumentsAreReported(string inputText, ErrorCode expectedErrorCode)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindModule(inputText, diagnosticBuilder);

        diagnosticBuilder.Build().Should().Contain(d => d.ErrorCode == expectedErrorCode && d.Level == DiagnosticLevel.Error);
    }

    [Theory]
    [InlineData("new void?[1]")]
    [InlineData("new string?[1]")]
    public void NullableElementTypesOfNewArraysAreValidated(string expression)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindExpression<BoundExpression>(expression, diagnosticBuilder);

        diagnosticBuilder.Build().Should().Contain(d => d.Level == DiagnosticLevel.Error);
    }

    [Fact]
    public void ConditionOnAnUntypedNullIsReportedWithoutThrowing()
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        TestUtils.BindStatement<BoundBlockStatement>("{ if null { } while null { } }", diagnosticBuilder);

        diagnosticBuilder.Build().Should().Contain(d => d.ErrorCode == ErrorCode.UntypedNullLiteral);
    }

    [Fact]
    public void NullableTypedConstantsAreConstants()
    {
        var module = TestUtils.BindModule("int Run() { const int? a = 1; return a.Value; } void Main() { }");

        var run = module.EntryPointType.BoundMembers.OfType<BoundFunctionMember>().Single(f => f.FunctionSymbol.Name == "Run");
        var declaration = run.Body.Statements[0].As<BoundVariableDeclarationStatement>();

        declaration.Variable.Constant.Should().BeTrue();
        declaration.InitializerExpression.Should().BeOfType<BoundConversionExpression>();
    }
}
