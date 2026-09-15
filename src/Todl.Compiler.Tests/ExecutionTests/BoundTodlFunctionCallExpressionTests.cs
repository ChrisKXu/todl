using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class BoundTodlFunctionCallExpressionTests
{
    [Fact]
    public void FunctionWithFiveOrMoreLocalsShouldEmitAndRunSuccessfully()
    {
        // Regression test: local variable slots at index 4+ crashed the emitter because
        // Ldloc_S/Ldloca_S were emitted with a raw slot index instead of the VariableDefinition
        // Mono.Cecil requires for that opcode form.
        var inputText = @"
import { Console } from System;

void Main() {}
int Run() {
    let a = 1;
    let b = 2;
    let c = 3;
    let d = 4;
    let e = 5;
    let f = 6;
    return a + b + c + d + e + f;
}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        diagnostics.Should().BeEmpty();
        assemblyDefinition.Should().NotBeNull();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();
            runMethod.Invoke(null, null).Should().Be(21);
        });
    }

    [Fact]
    public void MultiParameterFunctionCallShouldPassArgumentsInDeclaredOrder()
    {
        // Regression test: call arguments were emitted in the hash-iteration order of the
        // name-keyed BoundArguments dictionary rather than the function's declared parameter
        // order, silently swapping which value landed in which parameter slot.
        var inputText = @"
import { Console } from System;

void Main() {}
int Run() {
    return Combine(1000, 200, 30, 4);
}
int Combine(int a, int b, int c, int d) {
    return a - b - c - d;
}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        diagnostics.Should().BeEmpty();
        assemblyDefinition.Should().NotBeNull();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();
            runMethod.Invoke(null, null).Should().Be(1000 - 200 - 30 - 4);
        });
    }

    [Fact]
    public void InstanceMethodCallWithArgumentsShouldPushReceiverBeforeArguments()
    {
        // Regression test: instance CLR method call arguments were pushed onto the stack
        // before the receiver ("this"), producing an invalid evaluation stack for any
        // instance call that also takes arguments.
        var inputText = @"
import { Console } from System;

void Main() {}
string Run() {
    const s = ""hello world"";
    return s.Substring(6);
}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        diagnostics.Should().BeEmpty();
        assemblyDefinition.Should().NotBeNull();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();
            runMethod.Invoke(null, null).Should().Be("world");
        });
    }

    [Fact]
    public void InstanceMethodCallOnValueTypeParameterShouldPassReceiverByAddress()
    {
        // Regression test: a value-type function parameter used as an instance-call receiver
        // (e.g. `a.ToString()`) pushed the raw value instead of its managed pointer, corrupting
        // the evaluation stack for the callee.
        var inputText = @"
import { Console } from System;

void Main() {}
string Run() {
    return Describe(42);
}
string Describe(int a) {
    return a.ToString();
}
";
        var (assemblyDefinition, diagnostics) = TestUtils.Compile(SourceText.FromString(inputText));
        diagnostics.Should().BeEmpty();
        assemblyDefinition.Should().NotBeNull();

        TestUtils.RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();
            runMethod.Invoke(null, null).Should().Be("42");
        });
    }
}
