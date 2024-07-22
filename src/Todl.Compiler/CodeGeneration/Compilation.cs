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
    internal BoundModule MainModule { get; }
    public ClrTypeCache ClrTypeCache { get; }

    private readonly MetadataLoadContext metadataLoadContext;
    private readonly DiagnosticBag.Builder diagnosticBuilder = new();

    public Compilation(
        string assemblyName,
        Version version,
        IEnumerable<SourceText> sourceTexts,
        MetadataLoadContext metadataLoadContext)
    {
        if (string.IsNullOrEmpty(assemblyName))
        {
            throw new ArgumentNullException(nameof(assemblyName));
        }

        if (sourceTexts is null)
        {
            throw new ArgumentNullException(nameof(sourceTexts));
        }

        if (metadataLoadContext is null)
        {
            throw new ArgumentNullException(nameof(metadataLoadContext));
        }

        AssemblyName = assemblyName;
        Version = version;

        this.metadataLoadContext = metadataLoadContext;

        ClrTypeCache = ClrTypeCache.FromAssemblies(metadataLoadContext.GetAssemblies(), metadataLoadContext.CoreAssembly);

        var syntaxTrees = sourceTexts.Select(s => SyntaxTree.Parse(s, ClrTypeCache));
        MainModule = BoundModule.Create(ClrTypeCache, syntaxTrees.ToImmutableList());

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
        var emitter = Emitter.CreateAssemblyEmitter(this);
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
