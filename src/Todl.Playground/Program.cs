using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Todl.Playground.Compilation;
using Todl.Playground.Decompilation;
using Todl.Playground.Handlers;

namespace Todl.Playground;

sealed class Program
{
    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var services = builder.Services;
        services.AddTransient<AssemblyResolver>();
        services.AddTransient<CompilationProvider>();
        services.AddTransient<WebSocketRequestHandler>();
        services.AddTransient<ErrorRequestMessageHandler>();
        services.AddTransient<InfoRequestMessageHandler>();
        services.AddTransient<CompileRequestMessageHandler>();
        services.AddSingleton<DecompilerProviderResolver>();

        services.Configure<JsonSerializerOptions>(json =>
        {
            json.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false));
            json.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            json.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        var app = builder.Build();
        app.UseWebSockets(new WebSocketOptions()
        {
            KeepAliveInterval = TimeSpan.FromMinutes(2)
        });

        app.MapMethods("/api/connect", new[] { "GET", "CONNECT" }, (context) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var compilationRequestHandler = context.RequestServices.GetService<WebSocketRequestHandler>();
                return compilationRequestHandler.HandleRequestAsync(context, CancellationToken.None);
            }

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return Task.FromResult(context.Response);
        });

        app.Run();
    }
}
