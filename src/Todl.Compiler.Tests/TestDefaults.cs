using System;
using System.Linq;
using System.Reflection;
using Todl.Compiler.CodeAnalysis;

namespace Todl.Compiler.Tests;

static class TestDefaults
{
    public static readonly ClrTypeCache DefaultClrTypeCache;
    public static readonly MetadataLoadContext MetadataLoadContext;

    static TestDefaults()
    {
        var assemblyPaths = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Select(a => a.Location).Distinct().ToList();

        var resolver = new PathAssemblyResolver(assemblyPaths);
        MetadataLoadContext = new MetadataLoadContext(resolver, typeof(object).Assembly.GetName().FullName);

        foreach (var assemblyPath in assemblyPaths)
        {
            MetadataLoadContext.LoadFromAssemblyPath(assemblyPath);
        }

        DefaultClrTypeCache = ClrTypeCache.FromAssemblies(assemblies: MetadataLoadContext.GetAssemblies(), MetadataLoadContext.CoreAssembly);
    }
}
