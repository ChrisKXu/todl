using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler;
using System.IO;
using ICSharpCode.Decompiler.Metadata;

namespace Todl.Playground.Decompilation;

public class CSharpDecompilationProvider : IDecompilationProvider
{
    public static readonly DecompilerSettings CSharpDecompilerSettings = new(LanguageVersion.CSharp10_0);

    private readonly PEFile peFile;
    private readonly CSharpDecompiler decompiler;

    public CSharpDecompilationProvider(Stream assmeblyStream, IAssemblyResolver assemblyResolver)
    {
        peFile = new PEFile(string.Empty, assmeblyStream);
        decompiler = new CSharpDecompiler(peFile, assemblyResolver, CSharpDecompilerSettings);
    }

    public string Decompile()
    {
        return decompiler.DecompileWholeModuleAsString();
    }

    public void Dispose()
    {
        peFile?.Dispose();
    }
}
