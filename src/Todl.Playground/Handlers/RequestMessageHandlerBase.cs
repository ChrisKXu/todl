using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Todl.Playground.Handlers;

public abstract class RequestMessageHandlerBase : IDisposable
{
    protected readonly JsonSerializerOptions jsonSerializerOptions;

    public RequestMessageHandlerBase(JsonSerializerOptions jsonSerializerOptions)
    {
        this.jsonSerializerOptions = jsonSerializerOptions;
    }

    public abstract void Dispose();

    public abstract ValueTask HandlerRequestMessageAsync(WebSocket webSocket, RequestMessage requestMessage, CancellationToken cancellationToken);

    protected ValueTask SendResponseAsync<T>(WebSocket webSocket, T response, CancellationToken cancellationToken)
    {
        var responseString = JsonSerializer.Serialize(response, jsonSerializerOptions);
        var memory = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(responseString));
        return webSocket.SendAsync(memory, WebSocketMessageType.Text, true, cancellationToken);
    }
}
