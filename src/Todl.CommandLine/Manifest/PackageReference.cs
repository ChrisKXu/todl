using System.Text.Json.Serialization;

namespace Todl.CommandLine.Manifest;

// Accepts three shapes in the source JSON:
//   - a plain NuGet version string ("13.0.3")
//   - an expanded { "version": "13.0.3", "source": "https://..." } object
//   - a local project reference: a bare path string ("../MyLib") that fails
//     to parse as a NuGet version, or an explicit { "path": "../MyLib" } object
// Exactly one of Version/Path is ever set; enforced by PackageReferenceConverter.
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
