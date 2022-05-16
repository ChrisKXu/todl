using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Todl.Compiler.CodeAnalysis;

namespace Todl.Compiler.Tests;

static class TestDefaults
{
    public static readonly ClrTypeCache DefaultClrTypeCache;
    public static readonly IReadOnlyList<string> AssemblyPaths;

    static TestDefaults()
    {
        AssemblyPaths = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Select(a => a.Location).Distinct().ToList();

        var resolver = new PathAssemblyResolver(AssemblyPaths);
        var metadataLoadContext = new MetadataLoadContext(resolver, typeof(object).Assembly.GetName().FullName);

        DefaultClrTypeCache = ClrTypeCache.FromAssemblies(
            assemblies: AssemblyPaths.Select(metadataLoadContext.LoadFromAssemblyPath));
    }
}
