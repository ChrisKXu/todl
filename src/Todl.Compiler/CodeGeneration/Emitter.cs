using System.IO;

namespace Todl.Compiler.CodeGeneration;

internal class Emitter
{
    private readonly Compilation compilation;
    private readonly Stream stream;

    internal Emitter(Compilation compilation, Stream stream)
    {
        this.compilation = compilation;
        this.stream = stream;
    }

    public void Emit()
    {
        
    }
}
