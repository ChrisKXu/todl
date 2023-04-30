using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;
using Todl.Playground.Compilation;
using Todl.Playground.Decompilation;

namespace Todl.Playground.Handlers;

public class CompileRequestMessageHandler : RequestMessageHandlerBase
{
    private readonly DecompilerProviderResolver decompilerProviderResolver;
    private readonly AssemblyResolver assemblyResolver;
    private readonly CompilationProvider compilationProvider;

    public CompileRequestMessageHandler(
        IOptions<JsonSerializerOptions> jsonSerializerOptions,
        DecompilerProviderResolver decompilerProviderResolver,
        AssemblyResolver assemblyResolver,
        CompilationProvider compilationProvider)
        : base(jsonSerializerOptions.Value)
    {
        this.decompilerProviderResolver = decompilerProviderResolver;
        this.assemblyResolver = assemblyResolver;
        this.compilationProvider = compilationProvider;
    }

    public override void Dispose()
    {

    }

    public override ValueTask HandlerRequestMessageAsync(WebSocket webSocket, RequestMessage requestMessage, CancellationToken cancellationToken)
    {
        var compileRequest = requestMessage.CompileRequest;
        if (compileRequest is null || compileRequest.SourceFiles.IsDefaultOrEmpty)
        {
            return SendResponseAsync(webSocket, ErrorResponseMessage.Create("Invalid compile request"), cancellationToken);
        }

        try
        {
            var sourceTexts = compileRequest.SourceFiles.Select(s => new SourceText
            {
                FilePath = s.Name,
                Text = s.Content
            });

            using var compilation = compilationProvider.Compile(sourceTexts);
            var diagnostics = compilation.MainModule.GetDiagnostics();

            if (diagnostics.HasError())
            {
                return SendResponseAsync(webSocket, CompileResponseMessage.Create(string.Empty, diagnostics), cancellationToken);
            }

            var assemblyDefinition = compilation.Emit();
            using var stream = new MemoryStream();
            assemblyDefinition.Write(stream);

            // move stream to the beginning for decompiling
            stream.Position = 0;

            using var decompilationProvider = decompilerProviderResolver.Resolve(compileRequest.Type, assemblyResolver, stream);
            var decompiledString = decompilationProvider.Decompile();
            return SendResponseAsync(webSocket, CompileResponseMessage.Create(decompiledString, diagnostics), cancellationToken);
        }
        catch (Exception ex)
        {
            return SendResponseAsync(webSocket, ErrorResponseMessage.Create(ex.Message), cancellationToken);
        }
    }
}

public record CompileResponseMessage
(
    string Type,
    string DecompiledText,
    IEnumerable<Diagnostic> Diagnostics
)
{
    public static CompileResponseMessage Create(string decompiledText, IEnumerable<Diagnostic> diagnostics)
        => new("compile", decompiledText, diagnostics);
}
