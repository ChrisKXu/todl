using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Todl.Compiler.CodeAnalysis;

namespace Todl.Compiler.Tests.CodeAnalysis;

static class TestDefaults
{
    public static readonly ClrTypeCache DefaultClrTypeCache;

    static TestDefaults()
    {
        var paths = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Select(a => a.Location).Distinct().ToList();

        var resolver = new PathAssemblyResolver(paths);
        var metadataLoadContext = new MetadataLoadContext(resolver, typeof(object).Assembly.GetName().FullName);

        DefaultClrTypeCache = ClrTypeCache.FromAssemblies(
            assemblies: paths.Select(metadataLoadContext.LoadFromAssemblyPath));
    }
}
