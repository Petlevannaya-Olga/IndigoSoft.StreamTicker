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

public class KrakenWebSocketClient(
    string[] symbols,
    KrakenPolicies policies,
    ILogger<KrakenWebSocketClient> logger) : IWebSocketClient
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
                    await SubscribeAsync(ws, pollyCt);
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
        await ws.ConnectAsync(new Uri("wss://ws.kraken.com"), CancellationToken.None);
        logger.LogInformation("Connected to Kraken");
    }

    private async Task SubscribeAsync(ClientWebSocket ws, CancellationToken ct)
    {
        // Подписка на сделки для нескольких пар
        var subscribeMsg = new
        {
            @event = "subscribe",
            pair = symbols,
            subscription = new { name = "trade" }
        };

        await ws.SendAsync(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(subscribeMsg)),
            WebSocketMessageType.Text,
            true,
            ct);
    }

    private async Task ReceiveAsync(ClientWebSocket ws, ChannelWriter<Tick> writer, CancellationToken ct)
    {
        var buffer = new byte[16 * 1024];

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var message = await ReadMessageAsync(ws, buffer, ct);
            
            if (string.IsNullOrEmpty(message) || message.StartsWith($"{{")) 
                continue;
            
            // logger.LogInformation("Kraken message: {@Message}", message);

            try
            {
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;
                
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() >= 4)
                {
                    var tradesArray = root[1];
                    var pair = root[3].GetString();

                    foreach (var trade in tradesArray.EnumerateArray())
                    {
                        var price = double.Parse(trade[0].GetString()!, System.Globalization.CultureInfo.InvariantCulture);
                        var volume = double.Parse(trade[1].GetString()!, System.Globalization.CultureInfo.InvariantCulture);
                        var eventTime = (long)double.Parse(trade[2].GetString()!, System.Globalization.CultureInfo.InvariantCulture);

                        var tick = new Tick(
                            Guid.NewGuid(),
                            nameof(AvailableExchanges.Kraken),
                            pair!,
                            price,
                            volume,
                            eventTime);

                        await writer.WriteAsync(tick, ct);
                        // logger.LogInformation("{Pair,-8} | Price: {Price,-10} | Volume: {Volume,-10} | Time: {Date, -10}", pair, price, volume, date);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Json parse error: {@Message}", message);
            }
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