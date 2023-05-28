using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Todl.Playground.Compilation;
using Todl.Playground.Decompilation;
using Todl.Playground.Handlers;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
services.AddTransient<AssemblyResolver>();
services.AddTransient<CompilationProvider>();
services.AddTransient<CompileRequestMessageHandler>();
services.AddSingleton<DecompilerProviderResolver>();
services.AddSingleton<InfoRequestMessageHandler>();

services.Configure<JsonOptions>(json =>
{
    json.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false));
    json.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    json.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

services.AddCors();

var app = builder.Build();

app.MapGet("/api/info", (InfoRequestMessageHandler handler) => handler.HandleRequest());

app.MapPost("/api/compile", (CompileRequestMessageHandler handler, CompileRequest request) =>
{
    try
    {
        return Results.Json(handler.HandleRequest(request));
    }
    catch (ArgumentException argumentException)
    {
        return Results.Json(new { Error = argumentException.Message }, null, null, 400);
    }
    catch (Exception exception)
    {
        return Results.Json(new { Error = exception.Message }, null, null, 500);
    }
});

app.UseCors(cors =>
{
    cors.AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod();
});

app.Run();
