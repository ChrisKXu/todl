using ICSharpCode.Decompiler.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Todl.Playground.Decompilation;

namespace Todl.Playground;

sealed class Program
{
    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var services = builder.Services;
        services.AddControllers();
        services.AddTransient<AssemblyResolver>();
        services.AddSingleton<DecompilerProviderResolver>();

        var app = builder.Build();
        app.MapControllers();
        app.Run();
    }
}
