using System.Reflection;
using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;

namespace Todl.Playground.Controllers;

[Route("api/info")]
[ApiController]
public class InfoController : Controller
{
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();
    private static readonly string gitBranch = Environment.GetEnvironmentVariable("GIT_BRANCH");
    private static readonly string gitCommit = Environment.GetEnvironmentVariable("GIT_COMMIT");

#if DEBUG
    private const bool Debug = true;
#else
    private const bool Debug = false;
#endif

    public IActionResult Get()
        => Ok(new
        {
            RuntimeInfo = new
            {
                OSEnvironment = Environment.OSVersion,
                Runtime = RuntimeInformation.FrameworkDescription,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString()
            },
            BuildInfo = new
            {
                Debug = Debug,
                Version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion,
                GitBranch = gitBranch,
                GitCommit = gitCommit
            }
        });
}
