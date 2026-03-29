using System.Text.Json;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.MessageConverters;

public class BinanceMessageConverter(ILogger<BinanceMessageConverter> logger) : IMessageConverter
{
    public List<Tick>? Convert(string message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            logger.LogWarning("JSON is null or empty.");
            return null;
        }

        try
        {
            var result = JsonSerializer.Deserialize<BinanceTickDto>(message);
            return result is null
                ? null
                :
                [
                    new Tick(
                        nameof(AvailableExchanges.Binance),
                        result.Data.Symbol,
                        result.Data.PriceValue,
                        result.Data.VolumeValue,
                        result.Data.EventTime)
                ];
        }
        catch (JsonException ex)
        {
            logger.LogWarning("Failed to deserialize JSON: {Message}", message);
        }
        catch (FormatException ex)
        {
            logger.LogWarning("Invalid number/date format in JSON: {Message}", message);
        }
        catch (OverflowException ex)
        {
            logger.LogWarning("Numeric value in JSON is too large: {Message}", message);
        }
        catch (Exception ex)
        {
            logger.LogError("Unexpected error while parsing JSON: {Message}", message);
        }

        return null;
    }
}