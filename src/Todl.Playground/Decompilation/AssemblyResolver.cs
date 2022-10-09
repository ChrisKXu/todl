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
    public IEnumerable<string> AssemblyPaths { get; }

    private readonly PathAssemblyResolver pathAssemblyResolver;

    public AssemblyResolver()
    {
        AssemblyPaths = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Select(a => a.Location).Distinct().ToList();

        pathAssemblyResolver = new PathAssemblyResolver(AssemblyPaths);

        MetadataLoadContext = new MetadataLoadContext(pathAssemblyResolver);
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
