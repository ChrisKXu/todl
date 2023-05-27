using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;
using Todl.Playground.Compilation;
using Todl.Playground.Decompilation;

namespace Todl.Playground.Handlers;

public class CompileRequestMessageHandler
{
    private readonly DecompilerProviderResolver decompilerProviderResolver;
    private readonly AssemblyResolver assemblyResolver;
    private readonly CompilationProvider compilationProvider;

    public CompileRequestMessageHandler(
        DecompilerProviderResolver decompilerProviderResolver,
        AssemblyResolver assemblyResolver,
        CompilationProvider compilationProvider)
    {
        this.decompilerProviderResolver = decompilerProviderResolver;
        this.assemblyResolver = assemblyResolver;
        this.compilationProvider = compilationProvider;
    }

    public CompileResponseMessage HandleRequest(CompileRequest request)
    {
        if (request.SourceFiles.IsDefaultOrEmpty)
        {
            throw new ArgumentException("Please provide at least 1 source file.");
        }

        var sourceTexts = request.SourceFiles.Select(s => new SourceText
        {
            FilePath = s.Name,
            Text = s.Content
        });

        using var compilation = compilationProvider.Compile(sourceTexts);
        var diagnostics = compilation.MainModule.GetDiagnostics();

        if (diagnostics.HasError())
        {
            return new CompileResponseMessage(request.Type, string.Empty, diagnostics);
        }

        var assemblyDefinition = compilation.Emit();
        using var stream = new MemoryStream();
        assemblyDefinition.Write(stream);

        // move stream to the beginning for decompiling
        stream.Position = 0;

        using var decompilationProvider = decompilerProviderResolver.Resolve(request.Type, assemblyResolver, stream);
        var decompiledString = decompilationProvider.Decompile();
        return new CompileResponseMessage(request.Type, decompiledString, diagnostics);
    }
}

public enum CompileRequestType
{
    IL,
    CSharp
}

public record SourceFile(string Name, string Content);

public record CompileRequest(CompileRequestType Type, ImmutableArray<SourceFile> SourceFiles);

public record CompileResponseMessage
(
    CompileRequestType Type,
    string DecompiledText,
    IEnumerable<Diagnostic> Diagnostics
);
