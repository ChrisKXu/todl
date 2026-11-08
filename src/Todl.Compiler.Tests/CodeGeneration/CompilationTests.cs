using System;
using System.Linq;
using FluentAssertions;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.CodeGeneration;
using Todl.Compiler.Diagnostics;
using Xunit;

namespace Todl.Compiler.Tests.CodeGeneration;

public sealed class CompilationTests
{
    [Fact]
    public void CompilationWithSyntaxErrorsSkipsBindingAndDoesNotThrow()
    {
        // Prefix ++ without an operand: the parser reports
        // UnsupportedIncrementDecrementOperator and still fabricates a missing
        // identifier operand. Binding that tree used to throw ArgumentNullException.
        var compilation = new Compilation(
            assemblyName: "test",
            version: new Version(1, 0),
            sourceTexts: new[] { SourceText.FromString("void Main() { ++; }") },
            clrTypeCache: TestDefaults.DefaultClrTypeCache);

        compilation.MainModule.Should().BeNull();

        var diagnostics = compilation.GetDiagnostics().ToArray();
        diagnostics.Should().Contain(d => d.ErrorCode == ErrorCode.UnsupportedIncrementDecrementOperator);
        diagnostics.Should().NotContain(d => d.ErrorCode == ErrorCode.MissingEntryPoint);

        Action emit = () => compilation.Emit();
        emit.Should().Throw<InvalidOperationException>();
    }
}
