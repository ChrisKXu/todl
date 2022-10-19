using System;
using System.Collections.Generic;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Playground.Decompilation;

using TodlCompilation = Todl.Compiler.CodeGeneration.Compilation;

namespace Todl.Playground.Compilation;

public class CompilationProvider
{
    public const string AssemblyName = "test";
    public static readonly Version AssemblyVersion = new(1, 0);
    public static readonly TimeSpan DefaultCompileTimeout = TimeSpan.FromSeconds(5);

    private readonly AssemblyResolver assemblyResolver;

    public CompilationProvider(AssemblyResolver assemblyResolver)
    {
        this.assemblyResolver = assemblyResolver;
    }

    public TodlCompilation Compile(IEnumerable<SourceText> sourceTexts)
    {
        return new TodlCompilation(AssemblyName, AssemblyVersion, sourceTexts, assemblyResolver.MetadataLoadContext);
    }
}
