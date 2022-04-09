using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Todl.Compiler.CodeAnalysis;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;

namespace Todl.Compiler.CodeGeneration;

public sealed class Compilation : IDisposable
{
    public string TargetAssembly { get; }
    public BoundModule MainModule { get; }

    private readonly MetadataLoadContext metadataLoadContext;
    private readonly ClrTypeCache clrTypeCache;

    public Compilation(
        string targetAssembly,
        IEnumerable<SourceText> sourceTexts,
        IEnumerable<string> assemblyPaths)
    {
        if (string.IsNullOrEmpty(targetAssembly))
        {
            throw new ArgumentNullException(nameof(targetAssembly));
        }

        if (sourceTexts is null)
        {
            throw new ArgumentNullException(nameof(sourceTexts));
        }

        if (assemblyPaths is null)
        {
            throw new ArgumentNullException(nameof(assemblyPaths));
        }

        var resolver = new PathAssemblyResolver(assemblyPaths);
        metadataLoadContext = new MetadataLoadContext(resolver);

        TargetAssembly = targetAssembly;

        var assemblies = assemblyPaths.Select(metadataLoadContext.LoadFromAssemblyPath);
        clrTypeCache = ClrTypeCache.FromAssemblies(assemblies);

        var syntaxTrees = sourceTexts.Select(s => SyntaxTree.Parse(s, clrTypeCache));
        MainModule = BoundModule.Create(syntaxTrees.ToImmutableList());
    }

    public void Emit(Stream stream)
    {
        var emitter = new Emitter(this, stream);
        emitter.Emit();
    }

    public void Dispose()
    {
        metadataLoadContext?.Dispose();
    }
}
