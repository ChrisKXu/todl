using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Todl.Playground.Handlers;

public sealed class InfoRequestMessageHandler
{
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();
    private static readonly string gitBranch = Environment.GetEnvironmentVariable("GIT_BRANCH");
    private static readonly string gitCommit = Environment.GetEnvironmentVariable("GIT_COMMIT");

#if DEBUG
    private const bool Debug = true;
#else
    private const bool Debug = false;
#endif

    public object HandleRequest()
    {
        return new
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
        };
    }
}
