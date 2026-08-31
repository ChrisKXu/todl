using System;
using System.Collections.Immutable;

namespace Todl.CommandLine.References;

/// <summary>
/// The complete, conflict-resolved reference set for one compilation.
/// </summary>
public sealed record ResolvedReferences
{
    /// <summary>
    /// Paths handed to <see cref="System.Reflection.PathAssemblyResolver"/>.
    /// INVARIANT: exactly one entry per assembly simple name, all managed.
    /// <see cref="System.Reflection.MetadataLoadContext"/> throws
    /// <see cref="System.IO.FileLoadException"/> on duplicate identities.
    /// </summary>
    public required ImmutableArray<ResolvedAssembly> Compile { get; init; }

    /// <summary>
    /// <see cref="System.Reflection.MetadataLoadContext"/>'s coreAssemblyName.
    /// "System.Runtime". Explicit, not null: the ref pack ships an mscorlib
    /// FACADE at 4.0.0.0 and the default probe order is
    /// mscorlib -&gt; System.Runtime -&gt; netstandard, so null risks selecting the shim.
    /// </summary>
    public required string CoreAssemblyName { get; init; }

    /// <summary>
    /// For runtimeconfig.json. NOT derivable from <see cref="Compile"/> —
    /// ref-pack assemblies carry the band contract version (e.g. 10.0.0.0),
    /// not the framework version.
    /// </summary>
    public required FrameworkReference Framework { get; init; }

    /// <summary>
    /// Not supplied by the shared framework, so must be copied beside the
    /// output. Always empty in this milestone; populated once NuGet package
    /// resolution lands.
    /// </summary>
    public required ImmutableArray<ResolvedAssembly> RuntimeCopyLocal { get; init; }
}

public sealed record ResolvedAssembly
{
    public required string Path { get; init; }

    /// <summary>
    /// Assembly simple name — the de-dup key. From FrameworkList.xml's
    /// @AssemblyName for framework assemblies; from metadata for package
    /// ones. NOT Path.GetFileNameWithoutExtension: filename and assembly
    /// name can differ.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// AssemblyVersion. Primary conflict tie-break once package references exist.
    /// </summary>
    public required Version Version { get; init; }

    public required ReferenceOrigin Origin { get; init; }
}

public enum ReferenceOrigin
{
    Framework,
    Package,
    Project,
}

public sealed record FrameworkReference
{
    /// <summary>"Microsoft.NETCore.App"</summary>
    public required string Name { get; init; }

    /// <summary>Band floor, e.g. "10.0.0" — NOT the ref pack's patch version.</summary>
    public required string Version { get; init; }

    /// <summary>"net10.0"</summary>
    public required string Tfm { get; init; }
}
