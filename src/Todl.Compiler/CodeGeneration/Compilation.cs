using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Todl.Compiler.CodeAnalysis;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.Diagnostics;

namespace Todl.Compiler.CodeGeneration;

public sealed class Compilation : IDisposable, IDiagnosable
{
    public string AssemblyName { get; }
    public Version Version { get; }
    public BoundModule MainModule { get; }

    private readonly MetadataLoadContext metadataLoadContext;
    private readonly ClrTypeCache clrTypeCache;
    private readonly DiagnosticBag.Builder diagnosticBuilder = new();

    public Compilation(
        string assemblyName,
        Version version,
        IEnumerable<SourceText> sourceTexts,
        IEnumerable<string> assemblyPaths)
    {
        if (string.IsNullOrEmpty(assemblyName))
        {
            throw new ArgumentNullException(nameof(assemblyName));
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

        AssemblyName = assemblyName;
        Version = version;

        var assemblies = assemblyPaths.Select(metadataLoadContext.LoadFromAssemblyPath);
        clrTypeCache = ClrTypeCache.FromAssemblies(assemblies);

        var syntaxTrees = sourceTexts.Select(s => SyntaxTree.Parse(s, clrTypeCache));
        MainModule = BoundModule.Create(syntaxTrees.ToImmutableList());

        if (MainModule.EntryPoint is null)
        {
            diagnosticBuilder.Add(new Diagnostic()
            {
                Message = "Module does not have an entry point.",
                ErrorCode = ErrorCode.MissingEntryPoint,
                TextLocation = default,
                Level = DiagnosticLevel.Error
            });
        }
    }

    public AssemblyDefinition Emit()
    {
        var emitter = new Emitter(this);
        return emitter.Emit();
    }

    public void Dispose()
    {
        metadataLoadContext?.Dispose();
    }

    public IEnumerable<Diagnostic> GetDiagnostics()
    {
        diagnosticBuilder.Add(MainModule);
        return diagnosticBuilder.Build();
    }
}
