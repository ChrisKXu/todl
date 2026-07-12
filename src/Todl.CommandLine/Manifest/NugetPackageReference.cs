using System.Text.Json.Serialization;

namespace Todl.CommandLine.Manifest;

// Accepts either a plain version string ("13.0.3") or an expanded
// { "version": "13.0.3", "source": "https://..." } object in the source JSON.
[JsonConverter(typeof(NugetPackageReferenceConverter))]
public sealed record NugetPackageReference
{
    public required string Version { get; init; }

    public string? Source { get; init; }
}
