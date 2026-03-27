using System.Text.Json;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.MessageConverters;

public class KrakenMessageConverter(ILogger<KrakenMessageConverter> logger) : IMessageConverter
{
    public List<Tick>? Convert(string message, CancellationToken ct)
    {
        try
        {
            if (message.StartsWith('{'))
            {
                return null;
            }

            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() < 4)
                return null;

            var tradesArray = root[1];
            var pair = root[3].GetString();
            if (tradesArray.ValueKind != JsonValueKind.Array || string.IsNullOrEmpty(pair))
                return null;

            var ticks = new List<KrakenTickDto>();

            foreach (var trade in tradesArray.EnumerateArray())
            {
                try
                {
                    if (trade.GetArrayLength() < 3)
                        continue;

                    if (!double.TryParse(trade[0].GetString(),
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var price))
                        continue;

                    if (!double.TryParse(trade[1].GetString(),
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var volume))
                        continue;

                    if (!double.TryParse(trade[2].GetString(),
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var eventTimeDouble))
                        continue;

                    var eventTime = (long)eventTimeDouble;
                    ticks.Add(new KrakenTickDto(pair, price, volume, eventTime));
                }
                catch (Exception exTrade)
                {
                    logger.LogError(exTrade, "Error parsing trade: {@Trade}", trade);
                }
            }

            return ticks.Count > 0
                ? ticks.Select(tick => new Tick(
                    nameof(AvailableExchanges.Kraken),
                    tick.Symbol,
                    tick.Price,
                    tick.Volume,
                    tick.EventTime)).ToList()
                : null;
        }
        catch (Exception ex)
        {
            logger.LogError("Json parse error: {@Message}", message);
            return null;
        }
    }
}