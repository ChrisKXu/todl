using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Todl.Playground.Handlers;

public class WebSocketRequestHandler
{
    private const int BufferLength = 1024 * 4; // 4kb buffer size

    private readonly JsonSerializerOptions jsonSerializerOptions;

    public WebSocketRequestHandler(
        IOptions<JsonSerializerOptions> jsonSerializerOptions)
    {
        this.jsonSerializerOptions = jsonSerializerOptions.Value;
    }

    public async Task HandleRequestAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        using var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
        using var mempool = MemoryPool<byte>.Shared.Rent(BufferLength);

        while (webSocket.State == WebSocketState.Open)
        {
            var receiveResult = await webSocket.ReceiveAsync(mempool.Memory, cancellationToken);
            var requestMessage = TryDeserializeMessage(mempool.Memory.Slice(0, receiveResult.Count).Span);

            using (var handler = GetHandler(httpContext, requestMessage))
            {
                await handler.HandlerRequestMessageAsync(webSocket, requestMessage, cancellationToken);
            }
        }

        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
    }

    private RequestMessage TryDeserializeMessage(ReadOnlySpan<byte> buffer)
    {
        try
        {
            return JsonSerializer.Deserialize<RequestMessage>(buffer, jsonSerializerOptions);
        }
        catch
        {
            return null;
        }
    }

    private RequestMessageHandlerBase GetHandler(HttpContext httpContext, RequestMessage requestMessage)
    {
        return requestMessage switch
        {
            null => httpContext.RequestServices.GetService<ErrorRequestMessageHandler>(),
            _ => requestMessage.Type switch
            {
                RequestMessageType.Info => httpContext.RequestServices.GetService<InfoRequestMessageHandler>(),
                RequestMessageType.Compile => httpContext.RequestServices.GetService<CompileRequestMessageHandler>(),
                _ => httpContext.RequestServices.GetService<ErrorRequestMessageHandler>()
            }
        };
    }
}
