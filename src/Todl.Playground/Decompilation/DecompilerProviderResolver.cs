using System.IO;
using Todl.Playground.Models;

namespace Todl.Playground.Decompilation;

public class DecompilerProviderResolver
{
    public IDecompilationProvider Resolve(
        CompileRequestType compileRequestType,
        AssemblyResolver assemblyResolver,
        Stream assemblyStream)
    {
        return compileRequestType switch
        {
            CompileRequestType.CSharp => new CSharpDecompilationProvider(assemblyStream, assemblyResolver),
            _ => new ILDecompilationProvider(assemblyStream)
        };
    }
}
