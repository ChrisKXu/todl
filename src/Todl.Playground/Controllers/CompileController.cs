﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.CodeGeneration;
using Todl.Compiler.Diagnostics;
using Todl.Playground.Decompilation;
using Todl.Playground.Models;

namespace Todl.Playground.Controllers;

[Route("api/compile")]
[ApiController]
public class CompileController : ControllerBase
{
    public const string AssemblyName = "test";
    public static readonly Version AssemblyVersion = new Version(1, 0);

    private readonly DecompilerProviderResolver decompilerProviderResolver;
    private readonly AssemblyResolver assemblyResolver;

    public CompileController(
        DecompilerProviderResolver decompilerProviderResolver,
        AssemblyResolver assemblyResolver)
    {
        this.decompilerProviderResolver = decompilerProviderResolver;
        this.assemblyResolver = assemblyResolver;
    }

    public ActionResult<CompileResponse> Post(CompileRequest compileRequest)
    {
        compileRequest.Validate();

        var sourceTexts = compileRequest.SourceFiles.Select(s => new SourceText
        {
            FilePath = s.Name,
            Text = s.Content
        });

        using var compilation = new Compilation(AssemblyName, AssemblyVersion, sourceTexts, assemblyResolver.AssemblyPaths);
        var diagnostics = compilation.MainModule.GetDiagnostics();

        if (diagnostics.HasError())
        {
            return Ok(new CompileResponse(diagnostics, null, string.Empty));
        }

        var assemblyDefinition = compilation.Emit();
        using var stream = new MemoryStream();
        assemblyDefinition.Write(stream);

        // move stream to the beginning for decompiling
        stream.Position = 0;

        using var decompilationProvider = decompilerProviderResolver.Resolve(compileRequest.Type, assemblyResolver, stream);
        var decompiledString = decompilationProvider.Decompile();

        return Ok(new CompileResponse(diagnostics, null, decompiledString));
    }
}