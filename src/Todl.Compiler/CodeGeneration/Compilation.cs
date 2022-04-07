using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Todl.Compiler.CodeAnalysis.Binding;

namespace Todl.Compiler.CodeGeneration;

public sealed class Compilation
{
    public string TargetAssembly { get; private init; }
    public BoundModule MainModule { get; private init; }
    public IReadOnlyList<AssemblyName> ReferencedAssemblies { get; private init; }

    public void Emit(Stream stream)
    {
        var emitter = new Emitter(this, stream);
        emitter.Emit();
    }

    public static Compilation Create(
        string targetAssembly,
        BoundModule mainModule,
        IReadOnlyList<AssemblyName> referencedAssemblies)
    {
        if (string.IsNullOrEmpty(targetAssembly))
        {
            throw new ArgumentNullException(nameof(targetAssembly));
        }

        return new()
        {
            TargetAssembly = targetAssembly,
            MainModule = mainModule,
            ReferencedAssemblies = referencedAssemblies ?? throw new ArgumentNullException(nameof(referencedAssemblies))
        };
    }
}
