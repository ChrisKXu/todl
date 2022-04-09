using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Todl.Compiler.CodeAnalysis.Binding;
using Todl.Compiler.CodeAnalysis.Syntax;
using Todl.Compiler.CodeAnalysis.Text;
using Todl.Compiler.CodeGeneration;

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

            using var compilation = new Compilation(
                targetAssembly: IntermediateAssembly,
                sourceTexts: SourceFiles.Select(SourceText.FromFile),
                assemblyPaths: References);

            using var stream = File.OpenWrite(IntermediateAssembly);
            compilation.Emit(stream);

            Log.LogMessage("Done compiling Todl assembly {0}", IntermediateAssembly);

            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex);
            return false;
        }
    }
}
