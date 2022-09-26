using System.Reflection;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Todl.Playground;

sealed class Program
{
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();
    private static readonly string gitBranch = Environment.GetEnvironmentVariable("GIT_BRANCH");
    private static readonly string gitCommit = Environment.GetEnvironmentVariable("GIT_COMMIT");

    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/api/info", () => Results.Ok(new
        {
            RuntimeInfo = new
            {
                OSEnvironment = Environment.OSVersion,
                Runtime = RuntimeInformation.FrameworkDescription,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString()
            },
            BuildInfo = new
            {
                Debug = assembly.GetCustomAttribute<DebuggableAttribute>() is not null,
                Version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion,
                GitBranch = gitBranch,
                GitCommit = gitCommit
            }
        }));

        app.Run();
    }
}
