using System;
using System.Collections.Immutable;
using System.Linq;

namespace Todl.Playground.Models;

public enum CompileRequestType
{
    IL,
    CSharp
}

public record SourceFile(string Name, string Content)
{
    public void Validate()
    {
        if (string.IsNullOrEmpty(Name))
        {
            throw new ArgumentException("Invalid source file: Name cannot be empty");
        }

        if (string.IsNullOrEmpty(Content))
        {
            throw new ArgumentException("Invalid source file: Content cannot be empty");
        }
    }
}

public record CompileRequest(CompileRequestType Type, ImmutableArray<SourceFile> SourceFiles)
{
    public void Validate()
    {
        if (!SourceFiles.Any())
        {
            throw new ArgumentException("Invalid request: sourceFiles cannot be empty");
        }

        foreach (var s in SourceFiles)
        {
            s.Validate();
        }
    }
}
