using System.IO;
using System.Linq;

namespace Todl.CommandLine.Manifest;

/// <summary>
/// Resolves a <see cref="PackageReference.Path"/> value to the single .csproj
/// file inside the folder it points at. Not stored pre-resolved on the
/// manifest — the folder's .csproj contents can change between manifest
/// load and build.
/// </summary>
public static class ProjectReferenceResolver
{
    /// <param name="manifestDirectory">Absolute path to the directory containing todl.json.</param>
    /// <param name="manifestRelativePath">
    /// The manifest-authored path, e.g. "../MyLib" or "./2.0" — always
    /// '/'-delimited regardless of host OS, per todl.json's path convention.
    /// </param>
    /// <param name="referenceName">The manifest dictionary key, for diagnostics.</param>
    public static string ResolveCsprojPath(string manifestDirectory, string manifestRelativePath, string referenceName)
    {
        var nativeRelativePath = manifestRelativePath.Replace('/', Path.DirectorySeparatorChar);
        var projectDirectory = Path.GetFullPath(Path.Combine(manifestDirectory, nativeRelativePath));

        if (!Directory.Exists(projectDirectory))
        {
            throw new TodlManifestException(
                $"project reference '{referenceName}' points at '{projectDirectory}', which does not exist.");
        }

        // Non-recursive: a nested test project or sample under the referenced
        // folder must not be picked up by accident.
        var csprojFiles = Directory.GetFiles(projectDirectory, "*.csproj");

        if (csprojFiles.Length == 0)
        {
            throw new TodlManifestException(
                $"project reference '{referenceName}' points at '{projectDirectory}', which contains no .csproj file.");
        }

        if (csprojFiles.Length > 1)
        {
            throw new TodlManifestException(
                $"project reference '{referenceName}' points at '{projectDirectory}', which contains more than one .csproj file " +
                $"({string.Join(", ", csprojFiles.Select(Path.GetFileName))}); point at a more specific subfolder.");
        }

        return csprojFiles[0];
    }
}
