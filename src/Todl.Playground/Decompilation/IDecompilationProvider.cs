using System;

namespace Todl.Playground.Decompilation;

public interface IDecompilationProvider : IDisposable
{
    string Decompile();
}
