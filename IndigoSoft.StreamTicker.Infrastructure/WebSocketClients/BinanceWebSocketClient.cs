using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;
using IndigoSoft.StreamTicker.Domain;
using IndigoSoft.StreamTicker.Infrastructure.Policies;
using Microsoft.Extensions.Logging;
using Polly.Bulkhead;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;

public class BinanceWebSocketClient(
    string[] symbols,
    BinancePolicies policies,
    ILogger<BinanceWebSocketClient> logger)
    : IWebSocketClient
{
    public async Task RunAsync(ChannelWriter<Tick> writer, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await policies.WrapWebSocketPolicy().ExecuteAsync(async pollyCt =>
                {
                    using var ws = new ClientWebSocket();
                    await ConnectAsync(ws, pollyCt);
                    await ReceiveAsync(ws, writer, pollyCt);

                    throw new Exception("WebSocket disconnected unexpectedly");
                }, ct);
            }
            catch (Exception ex) when (ex is BrokenCircuitException
                                           or BulkheadRejectedException
                                           or TimeoutRejectedException)
            {
                logger.LogWarning(ex, "Polly policy triggered, pausing reconnect...");
                await Task.Delay(2000, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WebSocket error, retrying...");
                await Task.Delay(2000, ct);
            }
        }
    }

    private async Task ConnectAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var url = $"wss://stream.binance.com:9443/stream?streams={string.Join("@trade/", symbols).ToLower()}@trade";
        await ws.ConnectAsync(new Uri(url), ct);
        logger.LogInformation("Connected to Binance");
    }

    private async Task ReceiveAsync(ClientWebSocket ws, ChannelWriter<Tick> writer, CancellationToken ct)
    {
        var buffer = new byte[16 * 1024];

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var message = await ReadMessageAsync(ws, buffer, ct);
            if (string.IsNullOrEmpty(message)) continue;

            var wrapper = JsonSerializer.Deserialize<BinanceTickDto>(message);

            if (wrapper == null) continue;

            var tick = new Tick(
                Guid.NewGuid(),
                nameof(AvailableExchanges.Binance),
                wrapper.Data.Symbol,
                double.Parse(wrapper.Data.Price),
                double.Parse(wrapper.Data.Volume),
                wrapper.Data.EventTime);

            // logger.LogInformation("Raw tick {tick}", tick);
            await writer.WriteAsync(tick, ct);
        }
    }

    private static async Task<string?> ReadMessageAsync(ClientWebSocket ws, byte[] buffer, CancellationToken ct)
    {
        await using var ms = new MemoryStream();
        WebSocketReceiveResult result;

        do
        {
            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed by client", ct);
                return null;
            }

            ms.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(ms.ToArray());
    }
}