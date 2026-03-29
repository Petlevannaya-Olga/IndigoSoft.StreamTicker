using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IndigoSoft.StreamTicker.Tests.Units;

public class DefaultMessageReceiverTests
{
    [Fact]
    public async Task Should_Receive_And_Handle_Message()
    {
        var logger = new Mock<ILogger<DefaultMessageReceiver>>();

        var ws = new FakeWebSocket();
        ws.Enqueue("hello");

        var receiver = new DefaultMessageReceiver(logger.Object);

        string? received = null;

        await receiver.ReceiveAsync(
            ws,
            msg =>
            {
                received = msg;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        received.Should().Be("hello");
    }

    [Fact]
    public async Task Should_Ignore_Binary_Message()
    {
        var logger = new Mock<ILogger<DefaultMessageReceiver>>();

        var ws = new FakeWebSocket();
        ws.EnqueueBinary(new byte[] { 1, 2, 3 });

        var receiver = new DefaultMessageReceiver(logger.Object);

        var called = false;

        await receiver.ReceiveAsync(
            ws,
            _ =>
            {
                called = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        called.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Log_Error_When_Handler_Fails()
    {
        var logger = new Mock<ILogger<DefaultMessageReceiver>>();

        var ws = new FakeWebSocket();
        ws.Enqueue("test");

        var receiver = new DefaultMessageReceiver(logger.Object);

        var called = false;

        await receiver.ReceiveAsync(
            ws,
            _ =>
            {
                called = true;
                throw new Exception("fail");
            },
            CancellationToken.None);

        called.Should().BeTrue();

        logger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Error while processing WebSocket message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_Stop_When_Connection_Closed()
    {
        var logger = new Mock<ILogger<DefaultMessageReceiver>>();

        var ws = new FakeWebSocket();
        ws.CloseNext();

        var receiver = new DefaultMessageReceiver(logger.Object);

        var called = false;

        await receiver.ReceiveAsync(
            ws,
            _ =>
            {
                called = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        called.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Stop_When_Receive_Fails()
    {
        var logger = new Mock<ILogger<DefaultMessageReceiver>>();

        var ws = new FakeWebSocket();
        ws.ThrowOnNextReceive();

        var receiver = new DefaultMessageReceiver(logger.Object);

        var called = false;

        await receiver.ReceiveAsync(
            ws,
            _ =>
            {
                called = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        called.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Throw_When_Message_Too_Large()
    {
        var logger = new Mock<ILogger<DefaultMessageReceiver>>();

        var ws = new FakeWebSocket();
        ws.Enqueue(new string('a', 2 * 1024 * 1024));

        var receiver = new DefaultMessageReceiver(logger.Object);

        var act = async () =>
            await receiver.ReceiveAsync(ws, _ => Task.CompletedTask, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Buffer overflow while reading WebSocket message");
    }
}