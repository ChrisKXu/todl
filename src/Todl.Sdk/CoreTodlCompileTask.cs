using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.CodeGeneration;
using Todl.Compiler.Diagnostics;

namespace Todl.Sdk;

public sealed class CoreTodlCompileTask : Task
{
    [Required]
    public string IntermediateAssembly { get; set; }

    [Required]
    public string[] SourceFiles { get; set; }

    [Required]
    public string[] References { get; set; }

    public override bool Execute()
    {
        try
        {
            Log.LogMessage("Compiling Todl assembly {0}", IntermediateAssembly);

            var pathAssemblyResolver = new PathAssemblyResolver(References);
            var metadataLoadContext = new MetadataLoadContext(pathAssemblyResolver);

            foreach (var reference in References)
            {
                metadataLoadContext.LoadFromAssemblyPath(reference);
            }

            using var compilation = new Compilation(
                assemblyName: Path.GetFileNameWithoutExtension(IntermediateAssembly),
                version: new Version(1, 0),
                sourceTexts: SourceFiles.Select(SourceText.FromFile),
                metadataLoadContext: metadataLoadContext);

            var diagnostics = compilation.GetDiagnostics();

            foreach (var d in diagnostics)
            {
                switch (d.Level)
                {
                    case DiagnosticLevel.Error:
                        Log.LogError(d.Message);
                        break;
                    case DiagnosticLevel.Warning:
                        Log.LogWarning(d.Message);
                        break;
                    default:
                        Log.LogMessage(d.Message);
                        break;
                }
            }

            if (diagnostics.Any(d => d.Level == DiagnosticLevel.Error))
            {
                return false;
            }

            using var stream = File.OpenWrite(IntermediateAssembly);
            var assemblyDefinition = compilation.Emit();
            assemblyDefinition.Write(stream);

            Log.LogMessage("Done compiling Todl assembly {0}", IntermediateAssembly);

            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true);
            return false;
        }
    }
}
