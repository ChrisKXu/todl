using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Todl.Playground.Handlers;

public sealed class InfoRequestMessageHandler : RequestMessageHandlerBase
{
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();
    private static readonly string gitBranch = Environment.GetEnvironmentVariable("GIT_BRANCH");
    private static readonly string gitCommit = Environment.GetEnvironmentVariable("GIT_COMMIT");

#if DEBUG
    private const bool Debug = true;
#else
    private const bool Debug = false;
#endif

    public InfoRequestMessageHandler(IOptions<JsonSerializerOptions> jsonSerializerOptions) : base(jsonSerializerOptions.Value) { }

    public override void Dispose() { }

    public override ValueTask HandlerRequestMessageAsync(WebSocket webSocket, RequestMessage requestMessage, CancellationToken cancellationToken)
    {
        var response = new
        {
            Type = "info",
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

        return SendResponseAsync(webSocket, response, cancellationToken);
    }
}
