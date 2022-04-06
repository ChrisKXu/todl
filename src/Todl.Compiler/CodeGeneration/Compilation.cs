using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using Todl.Compiler.CodeAnalysis.Binding;

namespace Todl.Compiler.CodeGeneration;

public sealed class Compilation
{
    public string AssemblyName { get; private init; }
    public BoundModule MainModule { get; private init; }
    public IReadOnlyList<AssemblyReference> AssemblyReferences { get; private init; }

    public void Emit(Stream stream)
    {
        var emitter = new Emitter(this, stream);
        emitter.Emit();
    }

    public static Compilation Create(
        string assemblyName,
        BoundModule mainModule,
        IReadOnlyList<AssemblyReference> assemblyReferences)
    {
        return new()
        {
            AssemblyName = assemblyName,
            MainModule = mainModule,
            AssemblyReferences = assemblyReferences
        };
    }
}
