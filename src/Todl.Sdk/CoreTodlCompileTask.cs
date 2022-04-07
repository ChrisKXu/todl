using System;
using System.Collections.Generic;
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

            var referencedAssemblies = new List<AssemblyName>();
            foreach (var reference in References)
            {
                Log.LogMessage("Loading assembly reference {0}", reference);
                referencedAssemblies.Add(AssemblyName.GetAssemblyName(reference));
            }

            var syntaxTrees = SourceFiles.Select(s => SyntaxTree.Parse(SourceText.FromFile(s)));

            var compilation = Compilation.Create(
                targetAssembly: IntermediateAssembly,
                mainModule: BoundModule.Create(syntaxTrees.ToList()),
                referencedAssemblies: referencedAssemblies);

            compilation.Emit(null);

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
