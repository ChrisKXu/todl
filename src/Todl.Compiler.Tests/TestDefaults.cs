using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Todl.Compiler.CodeAnalysis;
using Todl.Compiler.CodeAnalysis.Binding;

namespace Todl.Compiler.Tests;

static class TestDefaults
{
    public static readonly ClrTypeCache DefaultClrTypeCache;
    public static readonly ConstantValueFactory ConstantValueFactory;
    public static readonly MetadataLoadContext MetadataLoadContext;

    static TestDefaults()
    {
        // Get all assemblies from the runtime directory to ensure all framework types are available
        // This is more robust than AppDomain.CurrentDomain.GetAssemblies() which only includes loaded assemblies
        var runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
        var runtimeAssemblies = Directory.GetFiles(runtimeDirectory, "*.dll");

        // Also include assemblies from the current AppDomain (for test project assemblies like TestClass)
        var appDomainAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => a.Location);

        var assemblyPaths = runtimeAssemblies
            .Concat(appDomainAssemblies)
            .Distinct()
            .ToList();

        var resolver = new PathAssemblyResolver(assemblyPaths);
        MetadataLoadContext = new MetadataLoadContext(resolver, typeof(object).Assembly.GetName().FullName);

        foreach (var assemblyPath in assemblyPaths)
        {
            try
            {
                MetadataLoadContext.LoadFromAssemblyPath(assemblyPath);
            }
            catch
            {
                // Some assemblies may fail to load (native, etc.) - skip them
            }
        }

        DefaultClrTypeCache = ClrTypeCache.FromAssemblies(assemblies: MetadataLoadContext.GetAssemblies(), MetadataLoadContext.CoreAssembly);
        ConstantValueFactory = new ConstantValueFactory(DefaultClrTypeCache.BuiltInTypes);
    }
}
