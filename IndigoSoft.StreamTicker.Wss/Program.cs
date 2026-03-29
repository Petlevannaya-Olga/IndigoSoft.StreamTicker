using System.Globalization;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    using var ws = await context.WebSockets.AcceptWebSocketAsync();
    Console.WriteLine("Client connected");

    var random = new Random();

    var symbols = new Dictionary<string, double>
    {
        { "BTCUSDT", 60000 },
        { "ETHUSDT", 3000 },
        { "BNBUSDT", 500 },
        { "SOLUSDT", 150 }
    };

    var ticksPerSecond = 200; // можно 500+
    var intervalMs = 1000 / ticksPerSecond;

    var sb = new StringBuilder(256);

    while (ws.State == WebSocketState.Open)
    {
        foreach (var kv in symbols)
        {
            var symbol = kv.Key;
            var price = kv.Value;

            // движение цены
            price += (random.NextDouble() - 0.5) * 5;
            symbols[symbol] = price;

            sb.Clear();

            sb.Append("{\"stream\":\"");
            sb.Append(symbol.ToLower());
            sb.Append("@ticker\",\"data\":{");

            sb.Append("\"q\":\"");
            sb.Append(price.ToString("F2", CultureInfo.InvariantCulture));
            sb.Append("\",");

            sb.Append("\"s\":\"");
            sb.Append(symbol);
            sb.Append("\",");

            sb.Append("\"p\":\"");
            sb.Append(price.ToString("F2", CultureInfo.InvariantCulture));
            sb.Append("\",");

            sb.Append("\"E\":");
            sb.Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            sb.Append("}}");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());

            await ws.SendAsync(
                bytes,
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        await Task.Delay(intervalMs);
    }

    Console.WriteLine("Client disconnected");
});

app.Run("http://localhost:5000");