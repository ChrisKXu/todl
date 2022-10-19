using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.Metadata;

namespace Todl.Playground.Decompilation;

public class AssemblyResolver : IAssemblyResolver
{
    public MetadataLoadContext MetadataLoadContext { get; }

    private readonly PathAssemblyResolver pathAssemblyResolver;

    public AssemblyResolver()
    {
        var assemblyPaths = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Select(a => a.Location).Distinct().ToList();

        pathAssemblyResolver = new PathAssemblyResolver(assemblyPaths);
        MetadataLoadContext = new MetadataLoadContext(pathAssemblyResolver);

        foreach (var assemblyPath in assemblyPaths)
        {
            MetadataLoadContext.LoadFromAssemblyPath(assemblyPath);
        }
    }

    public PEFile Resolve(IAssemblyReference reference)
    {
        var assemblyName = new AssemblyName(reference.FullName);
        var assembly = pathAssemblyResolver.Resolve(MetadataLoadContext, assemblyName);

        return new PEFile(assembly.Location);
    }

    public Task<PEFile> ResolveAsync(IAssemblyReference reference)
    {
        return Task.FromResult(Resolve(reference));
    }

    public PEFile ResolveModule(PEFile mainModule, string moduleName)
    {
        throw new NotSupportedException();
    }

    public Task<PEFile> ResolveModuleAsync(PEFile mainModule, string moduleName)
    {
        throw new NotSupportedException();
    }
}
