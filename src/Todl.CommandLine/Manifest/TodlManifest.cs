using System.Collections.Generic;

namespace Todl.CommandLine.Manifest;

public sealed record TodlManifest
{
    public string? Name { get; init; }

    public string Version { get; init; } = "0.1.0";

    public IReadOnlyDictionary<string, NugetPackageReference> NugetPackages { get; init; }
        = new Dictionary<string, NugetPackageReference>();
}
