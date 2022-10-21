using System;
using System.IO;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;

namespace Todl.Playground.Decompilation;

public class ILDecompilationProvider : IDecompilationProvider, IDisposable
{
    private readonly PEFile peFile;

    public ILDecompilationProvider(Stream assmeblyStream)
    {
        peFile = new PEFile(string.Empty, assmeblyStream);
    }

    public string Decompile()
    {
        using var writer = new StringWriter();
        var output = new PlainTextOutput(writer) { IndentationString = "    " };
        var disassembler = new ReflectionDisassembler(output, CancellationToken.None)
        {
            ShowSequencePoints = true
        };

        disassembler.WriteAssemblyHeader(peFile);
        output.WriteLine(); // empty line
        disassembler.WriteModuleContents(peFile);

        return writer.ToString();
    }

    public void Dispose()
    {
        peFile?.Dispose();
    }
}
