using System.Text.Json.Serialization;

namespace IndigoSoft.StreamTicker.Contracts;

public class BinanceTickDto
{
    [JsonPropertyName("stream")]
    public string Stream { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public BinanceDto Data { get; set; } = new();
}