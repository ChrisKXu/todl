using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Xml.Linq;

namespace Todl.CommandLine.References;

/// <summary>
/// Resolves the implicit .NET framework reference set — the assemblies the
/// MSBuild SDK used to supply for free via
/// <c>&lt;FrameworkReference Include="Microsoft.NETCore.App" IsImplicitlyDefined="true"&gt;</c>.
/// Todl only ever targets the framework the <c>todl</c> binary itself runs
/// on, so this reduces to: find the installed reference-assembly pack for
/// that band, and read its <c>data/FrameworkList.xml</c>.
/// </summary>
public static class FrameworkReferenceResolver
{
    private const string RefPackOverrideEnvVar = "TODL_REF_PACK";
    private const string DotnetRootEnvVar = "DOTNET_ROOT";
    private const string DotnetRootX86EnvVar = "DOTNET_ROOT(x86)";
    private const string PackName = "Microsoft.NETCore.App.Ref";
    private const string FrameworkReferenceName = "Microsoft.NETCore.App";
    private const string CoreAssemblyName = "System.Runtime";

    public static ResolvedReferences Resolve()
    {
        var (tfm, band) = GetTargetFramework();
        var packDir = FindPack(band)
            ?? throw new InvalidOperationException(BuildNotFoundMessage(band));

        var assemblies = ReadFrameworkList(packDir);

        var duplicates = assemblies
            .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToArray();

        if (duplicates.Length > 0)
        {
            // Cannot happen for a well-formed pack; a throw here is a real bug.
            throw new InvalidOperationException(
                $"Duplicate assembly names in '{packDir}': {string.Join(", ", duplicates.Select(g => g.Key))}");
        }

        return new ResolvedReferences
        {
            Compile = assemblies,
            CoreAssemblyName = CoreAssemblyName,
            Framework = new FrameworkReference
            {
                Name = FrameworkReferenceName,
                Version = $"{band.Major}.{band.Minor}.0", // band floor, not the pack patch
                Tfm = tfm,
            },
            RuntimeCopyLocal = ImmutableArray<ResolvedAssembly>.Empty,
        };
    }

    // ".NETCoreApp,Version=v10.0" -> ("net10.0", 10.0)
    internal static (string Tfm, Version Band) GetTargetFramework()
    {
        var attr = typeof(FrameworkReferenceResolver).Assembly
            .GetCustomAttribute<TargetFrameworkAttribute>()
            ?? throw new InvalidOperationException(
                "Todl.CommandLine has no TargetFrameworkAttribute; cannot determine the target framework.");

        var name = new FrameworkName(attr.FrameworkName);
        if (name.Identifier != ".NETCoreApp")
        {
            throw new InvalidOperationException($"Unsupported target framework identifier '{name.Identifier}'.");
        }

        var band = new Version(name.Version.Major, name.Version.Minor);
        return ($"net{band.Major}.{band.Minor}", band);
    }

    // Probe order, first hit wins:
    // 1. TODL_REF_PACK env var pointing directly at a pack directory (escape hatch, test seam).
    // 2. DOTNET_ROOT / DOTNET_ROOT(x86) env var.
    // 3. RuntimeEnvironment.GetRuntimeDirectory() + "../../.." — the reliable
    //    path for a framework-dependent todl.
    // 4. OS defaults.
    internal static string? FindPack(Version band)
    {
        var refPackOverride = Environment.GetEnvironmentVariable(RefPackOverrideEnvVar);
        if (!string.IsNullOrEmpty(refPackOverride) && Directory.Exists(refPackOverride))
        {
            return refPackOverride;
        }

        foreach (var dotnetRoot in CandidateDotnetRoots())
        {
            var found = TryFindPack(dotnetRoot, band);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    internal static string? TryFindPack(string dotnetRoot, Version band)
    {
        var packRoot = Path.Combine(dotnetRoot, "packs", PackName);
        if (!Directory.Exists(packRoot))
        {
            return null;
        }

        return Directory.EnumerateDirectories(packRoot)
            .Select(d => (Dir: d, Ver: Version.TryParse(Path.GetFileName(d), out var v) ? v : null))
            .Where(x => x.Ver is not null && x.Ver.Major == band.Major && x.Ver.Minor == band.Minor)
            .OrderByDescending(x => x.Ver)
            .Select(x => x.Dir)
            .FirstOrDefault();
    }

    internal static ImmutableArray<ResolvedAssembly> ReadFrameworkList(string packDir)
    {
        var listPath = Path.Combine(packDir, "data", "FrameworkList.xml");
        if (!File.Exists(listPath))
        {
            throw new InvalidOperationException($"'{listPath}' not found; the reference pack is incomplete.");
        }

        var root = XDocument.Load(listPath).Root
            ?? throw new InvalidOperationException($"'{listPath}' is empty or malformed.");

        return root.Elements("File")
            .Where(f => (string?)f.Attribute("Type") == "Managed") // Type="Analyzer" entries are Roslyn source generators; not references
            .Where(f => (string?)f.Attribute("ReferencedByDefault") != "false") // Absent from most packs, but honour it where present.
            .Select(f => new ResolvedAssembly
            {
                // Use @Path, not @AssemblyName + ".dll" — they can disagree.
                Path = Path.Combine(packDir, ((string)f.Attribute("Path")!).Replace('/', Path.DirectorySeparatorChar)),
                Name = (string)f.Attribute("AssemblyName")!,
                Version = Version.Parse((string)f.Attribute("AssemblyVersion")!),
                Origin = ReferenceOrigin.Framework,
            })
            .ToImmutableArray();
    }

    private static string BuildNotFoundMessage(Version band)
    {
        var refPackEnv = Environment.GetEnvironmentVariable(RefPackOverrideEnvVar);
        var dotnetRootEnv = Environment.GetEnvironmentVariable(DotnetRootEnvVar);

        var probeSummary = CandidateDotnetRoots()
            .Select(root =>
            {
                var packRoot = Path.Combine(root, "packs", PackName);
                if (!Directory.Exists(packRoot))
                {
                    return $"{packRoot} (not found)";
                }

                var found = string.Join(", ", Directory.EnumerateDirectories(packRoot).Select(Path.GetFileName));
                return $"{packRoot} (found: {found})";
            })
            .DefaultIfEmpty("<no candidate dotnet roots>")
            .First();

        return
            $"""
            error: no .NET {band.Major}.{band.Minor} reference pack found.
              probed: {RefPackOverrideEnvVar}={refPackEnv ?? "<unset>"}, {DotnetRootEnvVar}={dotnetRootEnv ?? "<unset>"},
                      {probeSummary}
              install the .NET {band.Major} SDK, or set {RefPackOverrideEnvVar} to a {PackName} directory.
            """;
    }

    private static System.Collections.Generic.IEnumerable<string> CandidateDotnetRoots()
    {
        var dotnetRoot = Environment.GetEnvironmentVariable(DotnetRootEnvVar)
            ?? Environment.GetEnvironmentVariable(DotnetRootX86EnvVar);
        if (!string.IsNullOrEmpty(dotnetRoot))
        {
            yield return dotnetRoot;
        }

        // The reliable path for a framework-dependent todl: walk up from the
        // running shared-framework directory. A self-contained todl would
        // instead yield its own app directory here, which simply won't
        // contain a packs/ folder and gets skipped by the caller.
        var runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
        var fromRuntimeDir = Path.GetFullPath(Path.Combine(runtimeDirectory, "..", "..", ".."));
        yield return fromRuntimeDir;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
            if (!string.IsNullOrEmpty(programFiles))
            {
                yield return Path.Combine(programFiles, "dotnet");
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            yield return "/usr/local/share/dotnet";
        }
        else
        {
            yield return "/usr/share/dotnet";
        }
    }
}
