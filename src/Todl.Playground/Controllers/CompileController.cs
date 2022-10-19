using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;
using Todl.Playground.Compilation;
using Todl.Playground.Decompilation;
using Todl.Playground.Models;

namespace Todl.Playground.Controllers;

[Route("api/compile")]
[ApiController]
public class CompileController : ControllerBase
{
    private readonly DecompilerProviderResolver decompilerProviderResolver;
    private readonly AssemblyResolver assemblyResolver;
    private readonly CompilationProvider compilationProvider;

    public CompileController(
        DecompilerProviderResolver decompilerProviderResolver,
        AssemblyResolver assemblyResolver,
        CompilationProvider compilationProvider)
    {
        this.decompilerProviderResolver = decompilerProviderResolver;
        this.assemblyResolver = assemblyResolver;
        this.compilationProvider = compilationProvider;
    }

    public ActionResult<CompileResponse> Post(CompileRequest compileRequest)
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
            return Ok(new CompileResponse(diagnostics, string.Empty));
        }

        var assemblyDefinition = compilation.Emit();
        using var stream = new MemoryStream();
        assemblyDefinition.Write(stream);

        // move stream to the beginning for decompiling
        stream.Position = 0;

        using var decompilationProvider = decompilerProviderResolver.Resolve(compileRequest.Type, assemblyResolver, stream);
        var decompiledString = decompilationProvider.Decompile();

        return Ok(new CompileResponse(diagnostics, decompiledString));
    }
}
