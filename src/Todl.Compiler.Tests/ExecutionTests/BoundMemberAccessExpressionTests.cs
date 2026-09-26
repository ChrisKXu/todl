using System;
using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.CodeGeneration;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.ExecutionTests;

public sealed class BoundMemberAccessExpressionTests
{
    // Regression test: an instance method invoked through a bare CLR type name (rather than
    // an instance expression) used to bind successfully with zero diagnostics and then crash
    // the code generator with an unhandled NotSupportedException the first time
    // Compilation.Emit() tried to emit the bare type name as a value. With the static/instance
    // context check in place, binding now reports a diagnostic, so callers such as
    // Todl.CommandLine's BuildCommand (which gates Emit() on diagnostics.HasError()) never
    // reach the emitter for this program in the first place.
    [Fact]
    public void InvokingInstanceMethodThroughBareTypeNameShouldReportDiagnosticInsteadOfReachingEmitter()
    {
        var inputText = @"
import { StringBuilder } from System::Text;

void Main() {}
string Run() {
    return StringBuilder.ToString();
}
";
        var compilation = new Compilation(
            assemblyName: "test",
            version: new Version(1, 0),
            sourceTexts: new[] { SourceText.FromString(inputText) },
            metadataLoadContext: TestDefaults.MetadataLoadContext);

        var diagnostics = compilation.GetDiagnostics().ToArray();

        diagnostics.Should().NotBeEmpty();
        diagnostics.Should().Contain(d => d.Level == DiagnosticLevel.Error);
    }
}
