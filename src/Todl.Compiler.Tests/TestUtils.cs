using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using FluentAssertions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.CodeGeneration;
using Todl.Compiler.Diagnostics;
using Todl.Compiler.Tests.CodeGeneration;

namespace Todl.Compiler.Tests;

internal static class TestUtils
{
    internal static TBoundExpression BindExpression<TBoundExpression>(
        string inputText, DiagnosticBag.Builder diagnosticBuilder)
        where TBoundExpression : BoundExpression
    {
        var expression = SyntaxTree.ParseExpression(SourceText.FromString(inputText), diagnosticBuilder);
        var binder = Binder.CreateModuleBinder(TestDefaults.DefaultClrTypeCache, TestDefaults.ConstantValueFactory, diagnosticBuilder);
        return binder.BindExpression(expression).As<TBoundExpression>();
    }

    internal static TBoundExpression BindExpression<TBoundExpression>(string inputText)
        where TBoundExpression : BoundExpression
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundExpression = BindExpression<TBoundExpression>(inputText, diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();
        return boundExpression;
    }

    internal static TBoundStatement BindStatement<TBoundStatement>(
        string inputText, DiagnosticBag.Builder diagnosticBuilder)
        where TBoundStatement : BoundStatement
    {
        var statement = SyntaxTree.ParseStatement(SourceText.FromString(inputText), diagnosticBuilder);
        var binder = Binder.CreateModuleBinder(TestDefaults.DefaultClrTypeCache, TestDefaults.ConstantValueFactory, diagnosticBuilder);
        return binder.BindStatement(statement).As<TBoundStatement>();
    }

    internal static TBoundStatement BindStatement<TBoundStatement>(string inputText)
        where TBoundStatement : BoundStatement
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundStatement = BindStatement<TBoundStatement>(inputText, diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();
        return boundStatement;
    }

    internal static TBoundMember BindMember<TBoundMember>(
        string inputText, DiagnosticBag.Builder diagnosticBuilder)
        where TBoundMember : BoundMember
    {
        var syntaxTree = ParseSyntaxTree(inputText);
        var binder = Binder.CreateModuleBinder(TestDefaults.DefaultClrTypeCache, TestDefaults.ConstantValueFactory, diagnosticBuilder);
        var member = syntaxTree.Members[0];

        if (member is FunctionDeclarationMember functionDeclarationMember)
        {
            binder.Scope.DeclareFunction(FunctionSymbol.FromFunctionDeclarationMember(functionDeclarationMember, binder.GetClrTypeCacheView(syntaxTree)));
        }

        return binder.BindMember(member).As<TBoundMember>();
    }

    internal static TBoundMember BindMember<TBoundMember>(string inputText)
        where TBoundMember : BoundMember
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundMember = BindMember<TBoundMember>(inputText, diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();
        return boundMember;
    }

    internal static BoundModule BindModule(string inputText, DiagnosticBag.Builder diagnosticBuilder)
    {
        var syntaxTree = ParseSyntaxTree(inputText, diagnosticBuilder);
        return BoundModule.Create(TestDefaults.DefaultClrTypeCache, [syntaxTree], diagnosticBuilder);
    }

    internal static BoundModule BindModule(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundModule = BindModule(inputText, diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();
        return boundModule;
    }

    internal static void EmitExpressionAndVerify(string input, params TestInstruction[] expectedInstructions)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundExpressionStatement = BindStatement<BoundExpressionStatement>(input, diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();

        var emitter = new TestEmitter();
        emitter.EmitStatement(boundExpressionStatement);
        emitter.Emit();

        emitter.ILProcessor.Body.Instructions.ShouldHaveExactInstructionSequence(expectedInstructions);
    }

    internal static void EmitStatementAndVerify(string input, params TestInstruction[] expectedInstructions)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var boundStatement = BindStatement<BoundStatement>(input, diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();

        var emitter = new TestEmitter();
        emitter.EmitStatement(boundStatement);
        emitter.Emit();

        emitter.ILProcessor.Body.Instructions.ShouldHaveExactInstructionSequence(expectedInstructions);
    }

    internal static (AssemblyDefinition, IEnumerable<Diagnostic>) Compile(SourceText sourceText)
    {
        var compilation = new Compilation(
            assemblyName: "test",
            version: new Version(1, 0),
            sourceTexts: new[] { sourceText },
            metadataLoadContext: TestDefaults.MetadataLoadContext);

        return (compilation.Emit(), compilation.GetDiagnostics());
    }

    internal static void RunAssembly(AssemblyDefinition assemblyDefinition, Action<System.Reflection.Assembly> action)
    {
        using var memoryStream = new MemoryStream();
        assemblyDefinition.Write(memoryStream);
        var assembly = System.Reflection.Assembly.Load(memoryStream.GetBuffer());

        action(assembly);
    }

    internal static SyntaxTree ParseSyntaxTree(string inputText, DiagnosticBag.Builder diagnosticBuilder)
        => SyntaxTree.Parse(SourceText.FromString(inputText), diagnosticBuilder);

    internal static SyntaxTree ParseSyntaxTree(string inputText)
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var syntaxTree = ParseSyntaxTree(inputText, diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();
        return syntaxTree;
    }

    internal static TExpression ParseExpression<TExpression>(string sourceText, DiagnosticBag.Builder diagnosticBuilder)
        where TExpression : Expression
        => SyntaxTree.ParseExpression(
            SourceText.FromString(sourceText),
            diagnosticBuilder).As<TExpression>();

    internal static TExpression ParseExpression<TExpression>(string sourceText)
        where TExpression : Expression
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var expression = ParseExpression<TExpression>(sourceText, diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();
        return expression;
    }

    internal static TStatement ParseStatement<TStatement>(string sourceText, DiagnosticBag.Builder diagnosticBuilder)
        where TStatement : Statement
        => SyntaxTree.ParseStatement(
            SourceText.FromString(sourceText),
            diagnosticBuilder).As<TStatement>();

    internal static TStatement ParseStatement<TStatement>(string sourceText)
        where TStatement : Statement
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var statement = ParseStatement<TStatement>(sourceText, diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();
        return statement;
    }

    internal static TDirective ParseDirective<TDirective>(string sourceText, DiagnosticBag.Builder diagnosticBuilder)
        where TDirective : Directive
        => SyntaxTree.Parse(
            SourceText.FromString(sourceText),
            diagnosticBuilder).Directives[0].As<TDirective>();

    internal static TDirective ParseDirective<TDirective>(string sourceText)
        where TDirective : Directive
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var directive = ParseDirective<TDirective>(sourceText, diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();
        return directive;
    }

    internal static TMember ParseMember<TMember>(string sourceText, DiagnosticBag.Builder diagnosticBuilder)
        where TMember : Member
        => SyntaxTree.Parse(
            SourceText.FromString(sourceText),
            diagnosticBuilder).Members[0].As<TMember>();

    internal static TMember ParseMember<TMember>(string sourceText)
        where TMember : Member
    {
        var diagnosticBuilder = new DiagnosticBag.Builder();
        var member = ParseMember<TMember>(sourceText, diagnosticBuilder);
        diagnosticBuilder.Build().Should().BeEmpty();
        return member;
    }

    internal static void ShouldHaveExactInstructionSequence(
        this Collection<Instruction> actualInstructions,
        params TestInstruction[] expectedInstructions)
    {
        actualInstructions.Select(TestInstruction.FromInstruction).Should().Equal(expectedInstructions);
    }
}

[DebuggerDisplay("{ToString()}")]
internal readonly struct TestInstruction : IEquatable<TestInstruction>
{
    public OpCode OpCode { get; private init; }

    public object Operand { get; private init; }

    public static TestInstruction Create(OpCode opCode)
        => new() { OpCode = opCode };

    public static TestInstruction Create(OpCode opCode, object operand)
        => new() { OpCode = opCode, Operand = operand };

    public static TestInstruction FromInstruction(Instruction instruction)
        => new()
        {
            OpCode = instruction.OpCode,
            Operand = instruction.Operand switch
            {
                VariableDefinition variableDefinition => variableDefinition.Index,
                FieldReference fieldReference => fieldReference.FullName,
                MethodReference methodReference => methodReference.FullName,
                Instruction innerInstruction => innerInstruction.Offset,
                _ => instruction.Operand
            }
        };

    public bool Equals(TestInstruction other)
        => OpCode.Equals(other.OpCode)
        && (Operand is null || Operand.Equals(other.Operand));

    public override int GetHashCode()
        => HashCode.Combine(OpCode, Operand);

    public override bool Equals([NotNullWhen(true)] object obj)
        => obj is TestInstruction other && Equals(other);

    public override string ToString()
    {
        if (Operand is null)
        {
            return $"({OpCode.Name})";
        }

        return $"({OpCode.Name}, {Operand})";
    }
}
