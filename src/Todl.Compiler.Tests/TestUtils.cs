using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Binding.BoundTree;
using Todl.Compiler.CodeAnalysis.Symbols;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;
using Todl.Compiler.Tests.CodeGeneration;

namespace Todl.Compiler.Tests;

internal static class TestUtils
{
    internal static TBoundExpression BindExpression<TBoundExpression>(
        string inputText, DiagnosticBag.Builder diagnosticBuilder)
        where TBoundExpression : BoundExpression
    {
        var expression = SyntaxTree.ParseExpression(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache);
        var binder = Binder.CreateModuleBinder(TestDefaults.DefaultClrTypeCache, diagnosticBuilder);
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
        var statement = SyntaxTree.ParseStatement(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache);
        var binder = Binder.CreateModuleBinder(TestDefaults.DefaultClrTypeCache, diagnosticBuilder);
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
        var binder = Binder.CreateModuleBinder(TestDefaults.DefaultClrTypeCache, diagnosticBuilder);
        var member = syntaxTree.Members[0];

        if (member is FunctionDeclarationMember functionDeclarationMember)
        {
            binder.Scope.DeclareFunction(FunctionSymbol.FromFunctionDeclarationMember(functionDeclarationMember));
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

    internal static SyntaxTree ParseSyntaxTree(string inputText)
        => SyntaxTree.Parse(SourceText.FromString(inputText), TestDefaults.DefaultClrTypeCache);

    internal static TExpression ParseExpression<TExpression>(string sourceText)
            where TExpression : Expression
    {
        return SyntaxTree.ParseExpression(SourceText.FromString(sourceText), TestDefaults.DefaultClrTypeCache).As<TExpression>();
    }

    internal static TStatement ParseStatement<TStatement>(string sourceText)
        where TStatement : Statement
    {
        return SyntaxTree.ParseStatement(SourceText.FromString(sourceText), TestDefaults.DefaultClrTypeCache).As<TStatement>();
    }

    internal static TDirective ParseDirective<TDirective>(string sourceText)
        where TDirective : Directive
    {
        var syntaxTree = SyntaxTree.Parse(SourceText.FromString(sourceText), TestDefaults.DefaultClrTypeCache);
        return syntaxTree.Directives[0].As<TDirective>();
    }

    internal static TMember ParseMember<TMember>(string sourceText)
        where TMember : Member
    {
        var syntaxTree = SyntaxTree.Parse(SourceText.FromString(sourceText), TestDefaults.DefaultClrTypeCache);
        return syntaxTree.Members[0].As<TMember>();
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
