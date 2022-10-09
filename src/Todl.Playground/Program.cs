using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Todl.Playground.Controllers;
using Todl.Playground.Decompilation;

namespace Todl.Playground;

sealed class Program
{
    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var services = builder.Services;
        services.AddTransient<AssemblyResolver>();
        services.AddSingleton<DecompilerProviderResolver>();

        services
            .AddControllers(options =>
            {
                options.Filters.Add(new ExceptionFilter());
            })
            .AddJsonOptions(json =>
            {
                json.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                json.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        var app = builder.Build();
        app.MapControllers();
        app.Run();
    }
}
