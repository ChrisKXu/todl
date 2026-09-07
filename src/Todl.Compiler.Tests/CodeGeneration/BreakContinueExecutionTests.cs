using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Mono.Cecil;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.CodeGeneration;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

// End-to-end proof that break/continue - including labeled variants - actually affect
// control flow at runtime, not just that they parse/bind/analyze cleanly. A wrong Emitter
// wiring here manifests as an infinite loop (timeout) rather than a clean assertion failure,
// so these tests are deliberately kept small and bounded.
public sealed class BreakContinueExecutionTests
{
    private static (AssemblyDefinition, IEnumerable<Diagnostic>) Compile(SourceText sourceText)
    {
        var compilation = new Compilation(
            assemblyName: "test",
            version: new Version(1, 0),
            sourceTexts: new[] { sourceText },
            metadataLoadContext: TestDefaults.MetadataLoadContext);

        return (compilation.Emit(), compilation.GetDiagnostics());
    }

    private static void RunAssembly(AssemblyDefinition assemblyDefinition, Action<Assembly> action)
    {
        using var memoryStream = new MemoryStream();
        assemblyDefinition.Write(memoryStream);
        var assembly = Assembly.Load(memoryStream.GetBuffer());

        action(assembly);
    }

    [Fact]
    public void BreakStatementStopsAnInfiniteLoop()
    {
        var inputText = @"
void Main() {}
int Run(int limit) {
    let sum = 0;
    let i = 0;
    while true {
        if i == limit {
            break;
        }
        sum = sum + i;
        i = i + 1;
    }
    return sum;
}
";
        var (assemblyDefinition, diagnostics) = Compile(SourceText.FromString(inputText));
        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();

            // 0 + 1 + 2 + 3 + 4 = 10. If `break` emitted no IL, this call would hang forever.
            runMethod.Invoke(null, new object[] { 5 }).Should().Be(10);
        });
    }

    [Fact]
    public void ContinueStatementSkipsTheRestOfTheLoopBody()
    {
        var inputText = @"
void Main() {}
int Run(int limit) {
    let sum = 0;
    let i = 0;
    while i != limit {
        i = i + 1;
        if i == 3 {
            continue;
        }
        sum = sum + i;
    }
    return sum;
}
";
        var (assemblyDefinition, diagnostics) = Compile(SourceText.FromString(inputText));
        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();

            // 1 + 2 + 4 + 5 = 12, skipping 3. If `continue` emitted no IL, this would add
            // every number instead (1+2+3+4+5=15).
            runMethod.Invoke(null, new object[] { 5 }).Should().Be(12);
        });
    }

    [Fact]
    public void LabeledBreakExitsAllTheWayOutOfTheOuterLoop()
    {
        var inputText = @"
void Main() {}
int Run() {
    let outerRuns = 0;
    let i = 0;
    while i != 5 : outer {
        i = i + 1;
        outerRuns = outerRuns + 1;
        let j = 0;
        while j != 5 : inner {
            j = j + 1;
            if j == 2 {
                break outer;
            }
        }
    }
    return outerRuns;
}
";
        var (assemblyDefinition, diagnostics) = Compile(SourceText.FromString(inputText));
        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();

            // `break outer;` fires on the outer loop's very first iteration, so outerRuns
            // must stay 1. If it only exited the inner loop (like a bare `break;` would),
            // the outer loop would keep going and outerRuns would reach 5.
            runMethod.Invoke(null, Array.Empty<object>()).Should().Be(1);
        });
    }

    [Fact]
    public void LabeledContinueReChecksTheOuterLoopNotTheInnerOne()
    {
        var inputText = @"
void Main() {}
int Run() {
    let outerCompletions = 0;
    let i = 0;
    while i != 3 : outer {
        i = i + 1;
        let j = 0;
        while j != 3 : inner {
            j = j + 1;
            if j == 1 {
                continue outer;
            }
        }
        outerCompletions = outerCompletions + 1;
    }
    return outerCompletions;
}
";
        var (assemblyDefinition, diagnostics) = Compile(SourceText.FromString(inputText));
        assemblyDefinition.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        RunAssembly(assemblyDefinition, assembly =>
        {
            var runMethod = assembly.EntryPoint.DeclaringType?.GetMethod("Run");
            runMethod.Should().NotBeNull();

            // `continue outer;` fires on the inner loop's first iteration (j == 1), every
            // outer iteration, so `outerCompletions` must never increment: 0. If it instead
            // re-checked the inner loop's own condition (like a bare `continue;` would), the
            // inner loop would run to completion each time and outerCompletions would reach 3.
            runMethod.Invoke(null, Array.Empty<object>()).Should().Be(0);
        });
    }
}
