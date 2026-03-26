using System.Text.Json;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.Parsers;

public class BinanceTickParser(ILogger<BinanceTickParser> logger) : IParser<BinanceTickDto>
{
    public BinanceTickDto? Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            logger.LogWarning("JSON is null or empty.");
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<BinanceTickDto>(json);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize JSON: {Message}", json);
        }
        catch (FormatException ex)
        {
            logger.LogWarning(ex, "Invalid number/date format in JSON: {Message}", json);
        }
        catch (OverflowException ex)
        {
            logger.LogWarning(ex, "Numeric value in JSON is too large: {Message}", json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while parsing JSON: {Message}", json);
        }

        return null;
    }
}