using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Todl.Playground.Models;

public enum CompileRequestType
{
    IL,
    CSharp
}

public class SourceFile
{
    [Required]
    public string Name { get; init; }

    [Required]
    public string Content { get; init; }
}

public class CompileRequest
{
    [Required]
    public CompileRequestType Type { get; init; }

    [Required]
    public ImmutableArray<SourceFile> SourceFiles { get; init; }
}
