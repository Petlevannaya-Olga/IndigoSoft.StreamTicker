using System.Net.WebSockets;
using System.Threading.Tasks.Dataflow;
using FluentAssertions;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;
using Microsoft.Extensions.Logging;
using Moq;

namespace IndigoSoft.StreamTicker.Tests.Units;

public class DataflowWebSocketClientTests
{
    private Tick CreateTick(
        string symbol = "AAPL",
        double price = 100,
        double volume = 10,
        long eventTime = 1000)
    {
        return new Tick("Exchange", symbol, price, volume, eventTime);
    }

    [Fact]
    public async Task Should_Handle_Cancellation_Gracefully()
    {
        // Arrange
        var connector = new Mock<IWebSocketConnector>();
        var receiver = new Mock<IMessageReceiver>();
        var converter = new Mock<IMessageConverter>();
        var policy = new Mock<IWebSocketPolicy>();
        var logger = new Mock<ILogger<DataflowWebSocketClient>>();

        var target = new BufferBlock<Tick>();

        policy.Setup(p => p.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(async (func, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                await func(ct);
            });

        var client = new DataflowWebSocketClient(
            connector.Object,
            receiver.Object,
            converter.Object,
            policy.Object,
            logger.Object);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await client.RunAsync(target, cts.Token);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Should_LogError_When_Unexpected_Exception()
    {
        // Arrange
        var connector = new Mock<IWebSocketConnector>();
        var receiver = new Mock<IMessageReceiver>();
        var converter = new Mock<IMessageConverter>();
        var policy = new Mock<IWebSocketPolicy>();
        var logger = new Mock<ILogger<DataflowWebSocketClient>>();

        var target = new BufferBlock<Tick>();

        policy.Setup(p => p.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var client = new DataflowWebSocketClient(
            connector.Object,
            receiver.Object,
            converter.Object,
            policy.Object,
            logger.Object);

        // Act
        await client.RunAsync(target, CancellationToken.None);

        // Assert
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Should_Send_Converted_Items_To_Target()
    {
        // Arrange
        var connector = new Mock<IWebSocketConnector>();
        var receiver = new Mock<IMessageReceiver>();
        var converter = new Mock<IMessageConverter>();
        var policy = new Mock<IWebSocketPolicy>();
        var logger = new Mock<ILogger<DataflowWebSocketClient>>();

        var target = new BufferBlock<Tick>();

        var ws = new Mock<IWebSocketConnection>();

        var messageHandled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        connector.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ws.Object);

        receiver.Setup(x => x.ReceiveAsync(
                ws.Object,
                It.IsAny<Func<string, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (IWebSocketConnection _, Func<string, Task> handler, CancellationToken ct) =>
            {
                await handler("test message");
                messageHandled.TrySetResult(true);
            });

        var ticks = new[] { CreateTick(), CreateTick() };

        converter.Setup(x => x.Convert(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, CancellationToken _) => ticks.ToList());

        policy.Setup(x => x.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(async (func, ct) => { await func(ct); });

        var client = new DataflowWebSocketClient(
            connector.Object,
            receiver.Object,
            converter.Object,
            policy.Object,
            logger.Object);

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = client.RunAsync(target, cts.Token);

        var completed = await Task.WhenAny(
            messageHandled.Task,
            Task.Delay(TimeSpan.FromSeconds(1)));

        completed.Should().Be(messageHandled.Task);

        await messageHandled.Task;

        // Assert
        var items = new List<Tick>();

        while (items.Count < 2)
        {
            var item = await target.ReceiveAsync(TimeSpan.FromSeconds(1));
            items.Add(item);
        }

        items.Should().HaveCount(2);

        // Cleanup
        cts.Cancel();
        await runTask;
    }
}