using System.Text.Json.Serialization;

namespace Todl.CommandLine.Manifest;

// A NuGet reference is a version string ("13.0.3") or { version, source? }
// object. A local project reference is a bare path string that fails to
// parse as a NuGet version ("../MyLib"); prefix "./" if the folder name
// would otherwise parse as one (e.g. "./2.0"). Exactly one of Version/Path
// is ever set — enforced by PackageReferenceConverter.
[JsonConverter(typeof(PackageReferenceConverter))]
public sealed record PackageReference
{
    public string? Version { get; init; }

    public string? Source { get; init; }

    /// <summary>
    /// Manifest-relative, '/'-delimited path to the folder containing the
    /// referenced project's .csproj (not the .csproj file itself). Null for
    /// a NuGet reference.
    /// </summary>
    public string? Path { get; init; }
}
