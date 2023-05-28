using System;
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

        // for whatever reason the decompiler tries to find mscorlib which we don't actually need
        if (assembly is null)
        {
            return null;
        }

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
