using System.Globalization;
using System.Text.Json;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.MessageConverters;

public class ByBitMessageConverter(ILogger<ByBitMessageConverter> logger) : IMessageConverter<Tick>
{
    public List<Tick>? Convert(string message, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            // системные сообщения
            if (root.TryGetProperty("op", out var op))
            {
                return null;
            }

            // проверяем наличие topic
            if (!root.TryGetProperty("topic", out var topic))
                return null;
            
            // ts
            if (!root.TryGetProperty("ts", out var ts))
                return null;
 
            var tsValue = long.Parse(ts.ToString());
            var topicStr = topic.GetString();

            if (topicStr != null && topicStr.StartsWith("tickers"))
            {
                if (!root.TryGetProperty("data", out var dataElement))
                    return null;

                // data может быть массивом ИЛИ объектом
                JsonElement ticker;

                if (dataElement.ValueKind == JsonValueKind.Array)
                {
                    ticker = dataElement[0];
                }
                else if (dataElement.ValueKind == JsonValueKind.Object)
                {
                    ticker = dataElement;
                }
                else
                {
                    return null;
                }

                if (ticker.TryGetProperty("symbol", out var symbolProp) &&
                    ticker.TryGetProperty("lastPrice", out var priceProp) &&
                    ticker.TryGetProperty("volume24h", out var volumeProp))
                {
                    var symbol = symbolProp.GetString();
                    var price = double.Parse(priceProp.GetString()!, CultureInfo.InvariantCulture);
                    var volume = double.Parse(volumeProp.GetString()!, CultureInfo.InvariantCulture);
                    return [new Tick(nameof(AvailableExchanges.ByBit), symbol!, price, volume, tsValue)];
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Bybit parse error: {ex.Message}");
        }

        return null;
    }
}