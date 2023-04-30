using System.Collections.Immutable;

namespace Todl.Playground.Handlers;

public enum RequestMessageType
{
    Error,
    Info,
    Compile
}

public record RequestMessage
(
    RequestMessageType Type,
    CompileRequest CompileRequest
);

public enum CompileRequestType
{
    IL,
    CSharp
}

public record SourceFile(string Name, string Content);

public record CompileRequest(CompileRequestType Type, ImmutableArray<SourceFile> SourceFiles);
