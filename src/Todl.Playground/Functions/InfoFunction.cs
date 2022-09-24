using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Todl.Playground.Functions;

public class InfoFunction
{
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();
    private static readonly string gitBranch = Environment.GetEnvironmentVariable("GIT_BRANCH");
    private static readonly string gitCommit = Environment.GetEnvironmentVariable("GIT_COMMIT");

    [FunctionName("info")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest request,
        ILogger logger)
    {
        return new OkObjectResult(new
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
        });
    }
}
