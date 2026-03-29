using System.Globalization;
using System.Text.Json.Serialization;

namespace IndigoSoft.StreamTicker.Contracts;

public class BinanceDto
{
    [JsonPropertyName("s")] public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("p")] public string Price { get; set; } = string.Empty;

    [JsonPropertyName("q")] public string Volume { get; set; } = string.Empty;

    [JsonPropertyName("E")] public long EventTime { get; set; }

    public double VolumeValue => double.Parse(Volume, CultureInfo.InvariantCulture);
    public double PriceValue => double.Parse(Price, CultureInfo.InvariantCulture);
}