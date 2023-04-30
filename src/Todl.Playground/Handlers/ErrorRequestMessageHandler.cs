using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Todl.Playground.Handlers;

public sealed class ErrorRequestMessageHandler : RequestMessageHandlerBase
{
    public ErrorRequestMessageHandler(IOptions<JsonSerializerOptions> jsonSerializerOptions) : base(jsonSerializerOptions.Value) { }

    public override ValueTask HandlerRequestMessageAsync(
        WebSocket webSocket,
        RequestMessage requestMessage,
        CancellationToken cancellationToken)
    {
        return SendResponseAsync(webSocket, ErrorResponseMessage.Create("Invalid request message"), cancellationToken);
    }

    public override void Dispose() { }
}

public record ErrorResponseMessage
(
    string Type,
    string Error
)
{
    public static ErrorResponseMessage Create(string errorMessage)
        => new("error", errorMessage);
}
