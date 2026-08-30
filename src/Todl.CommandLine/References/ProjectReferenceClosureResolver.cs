using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Todl.CommandLine.References;

/// <summary>
/// Turns a built referenced project into the flat list of assemblies it
/// contributes to the referencing compilation: its own primary output, plus
/// its transitive package/project dependency closure read from
/// <c>.deps.json</c> (<c>BuiltProjectOutputGroup</c> alone returns only the
/// primary output, not the closure — verified in
/// todl-design/ideas/tooling/project-file-format.md). No
/// <c>Microsoft.Build.*</c> type is touched here, so this class carries none
/// of <see cref="MSBuildBootstrap"/>'s ordering constraint.
/// </summary>
internal static class ProjectReferenceClosureResolver
{
    public static ImmutableArray<ResolvedAssembly> ResolveClosure(ProjectBuildResult buildResult)
    {
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var assemblies = ImmutableArray.CreateBuilder<ResolvedAssembly>();

        AddAssembly(buildResult.TargetPath, seenPaths, assemblies);

        var depsJsonPath = Path.ChangeExtension(buildResult.TargetPath, ".deps.json");
        if (File.Exists(depsJsonPath))
        {
            foreach (var path in ReadDepsJsonRuntimeAssetPaths(depsJsonPath, buildResult))
            {
                AddAssembly(path, seenPaths, assemblies);
            }
        }

        return assemblies.ToImmutable();
    }

    private static void AddAssembly(string path, HashSet<string> seenPaths, ImmutableArray<ResolvedAssembly>.Builder assemblies)
    {
        if (!File.Exists(path) || !seenPaths.Add(Path.GetFullPath(path)))
        {
            return;
        }

        AssemblyName assemblyName;
        try
        {
            assemblyName = AssemblyName.GetAssemblyName(path);
        }
        catch (BadImageFormatException)
        {
            // Not a managed assembly (a native asset that slipped through, or a
            // resource-only satellite) — not something MetadataLoadContext can
            // load, and not something to fail the whole build over.
            return;
        }

        assemblies.Add(new ResolvedAssembly
        {
            Path = path,
            Name = assemblyName.Name ?? Path.GetFileNameWithoutExtension(path),
            Version = assemblyName.Version ?? new Version(0, 0, 0, 0),
            Origin = ReferenceOrigin.Project,
        });
    }

    // Reads targets["<runtimeTarget>"][*]["runtime"] asset paths, resolving each
    // library against the global packages folder (type: "package") or the
    // referenced project's own output directory (type: "project" — nested
    // ProjectReferences that are copy-local into it). Deliberately reads only
    // "runtime" (not "compile"): Todl has no ref/impl split for package or
    // project assemblies, so the implementation assembly serves as both the
    // compile-time reference and the runtime copy-local artifact.
    private static IEnumerable<string> ReadDepsJsonRuntimeAssetPaths(string depsJsonPath, ProjectBuildResult buildResult)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(depsJsonPath));
        var root = document.RootElement;

        if (!root.TryGetProperty("runtimeTarget", out var runtimeTargetElement) ||
            !runtimeTargetElement.TryGetProperty("name", out var targetNameElement) ||
            targetNameElement.GetString() is not { } targetName ||
            !root.TryGetProperty("targets", out var targetsElement) ||
            !targetsElement.TryGetProperty(targetName, out var targetElement) ||
            !root.TryGetProperty("libraries", out var librariesElement))
        {
            yield break;
        }

        foreach (var libraryProperty in targetElement.EnumerateObject())
        {
            if (!libraryProperty.Value.TryGetProperty("runtime", out var runtimeElement))
            {
                continue;
            }

            if (!librariesElement.TryGetProperty(libraryProperty.Name, out var libraryMetadata) ||
                !libraryMetadata.TryGetProperty("type", out var typeElement))
            {
                continue;
            }

            var baseDirectory = typeElement.GetString() switch
            {
                "package" when buildResult.NuGetPackageRoot is not null
                    => Path.Combine(buildResult.NuGetPackageRoot, GetPackageRelativeDirectory(libraryMetadata, libraryProperty.Name)),
                "project" => buildResult.OutputDirectory,
                _ => null,
            };

            if (baseDirectory is null)
            {
                continue;
            }

            foreach (var asset in runtimeElement.EnumerateObject())
            {
                // "_._" is .deps.json's placeholder for "no assets of this type".
                if (asset.Name.EndsWith("_._", StringComparison.Ordinal))
                {
                    continue;
                }

                yield return Path.Combine(baseDirectory, asset.Name.Replace('/', Path.DirectorySeparatorChar));
            }
        }
    }

    // "path" (e.g. "mono.cecil/0.11.6") is the documented, present-in-practice
    // shape; falling back to the lowercased library key matches the global
    // packages folder's own on-disk naming convention if it's ever absent.
    private static string GetPackageRelativeDirectory(JsonElement libraryMetadata, string libraryKey)
    {
        var relativePath = libraryMetadata.TryGetProperty("path", out var pathElement)
            ? pathElement.GetString()
            : null;

        return (relativePath ?? libraryKey.ToLowerInvariant()).Replace('/', Path.DirectorySeparatorChar);
    }
}
