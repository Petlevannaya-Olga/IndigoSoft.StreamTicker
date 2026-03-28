using IndigoSoft.StreamTicker.Application;

namespace IndigoSoft.StreamTicker.Tests.Units;

using Domain;
using Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Threading.Tasks.Dataflow;

public class DataflowPipelineBackgroundServiceTests
{
    /*[Fact]
    public async Task Should_Save_Ticks_In_Batch()
    {
        // Arrange
        var ticks = Enumerable.Range(0, 1000)
            .Select(i => new Tick { Symbol = $"SYM{i}", Price = i })
            .ToArray();

        var repoMock = new Mock<ITickRepository>();
        var dedupMock = new Mock<IDeduplicator>();
        var metricsMock = new Mock<IMetricsService>();
        var loggerMock = new Mock<ILogger<DataflowPipelineBackgroundService>>();

        dedupMock.Setup(d => d.IsDuplicate(It.IsAny<Tick>()))
                 .Returns(false);

        var tcs = new TaskCompletionSource();

        repoMock.Setup(r => r.SaveBatchAsync(It.IsAny<Tick[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback(() => tcs.TrySetResult());

        var clientMock = new Mock<IWebSocketClient>();

        clientMock.Setup(c => c.RunAsync(It.IsAny<ITargetBlock<Tick>>(), It.IsAny<CancellationToken>()))
                  .Returns(async (ITargetBlock<Tick> target, CancellationToken ct) =>
                  {
                      foreach (var tick in ticks)
                          await target.SendAsync(tick, ct);

                      target.Complete();
                  });

        var service = new DataflowPipelineBackgroundService(
            new[] { clientMock.Object },
            repoMock.Object,
            dedupMock.Object,
            metricsMock.Object,
            loggerMock.Object);

        // Act
        var runTask = service.StartAsync(CancellationToken.None);

        await Task.WhenAny(runTask, Task.Delay(5000));

        // Assert
        repoMock.Verify(r => r.SaveBatchAsync(It.IsAny<Tick[]>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        metricsMock.Verify(m => m.Increment(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Should_Skip_Duplicates()
    {
        // Arrange
        var repoMock = new Mock<ITickRepository>();
        var dedupMock = new Mock<IDeduplicator>();
        var metricsMock = new Mock<IMetricsService>();
        var loggerMock = new Mock<ILogger<DataflowPipelineBackgroundService>>();

        dedupMock.Setup(d => d.IsDuplicate(It.IsAny<Tick>()))
                 .Returns(true); // всё дублируется

        var clientMock = new Mock<IWebSocketClient>();

        clientMock.Setup(c => c.RunAsync(It.IsAny<ITargetBlock<Tick>>(), It.IsAny<CancellationToken>()))
                  .Returns(async (ITargetBlock<Tick> target, CancellationToken ct) =>
                  {
                      for (int i = 0; i < 100; i++)
                          await target.SendAsync(new Tick());

                      target.Complete();
                  });

        var service = new DataflowPipelineBackgroundService(
            new[] { clientMock.Object },
            repoMock.Object,
            dedupMock.Object,
            metricsMock.Object,
            loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        repoMock.Verify(r => r.SaveBatchAsync(It.IsAny<Tick[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Continue_When_Save_Fails()
    {
        // Arrange
        var repoMock = new Mock<ITickRepository>();
        var dedupMock = new Mock<IDeduplicator>();
        var metricsMock = new Mock<IMetricsService>();
        var loggerMock = new Mock<ILogger<DataflowPipelineBackgroundService>>();

        dedupMock.Setup(d => d.IsDuplicate(It.IsAny<Tick>()))
                 .Returns(false);

        repoMock.Setup(r => r.SaveBatchAsync(It.IsAny<Tick[]>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB error"));

        var clientMock = new Mock<IWebSocketClient>();

        clientMock.Setup(c => c.RunAsync(It.IsAny<ITargetBlock<Tick>>(), It.IsAny<CancellationToken>()))
                  .Returns(async (ITargetBlock<Tick> target, CancellationToken ct) =>
                  {
                      await target.SendAsync(new Tick());
                      target.Complete();
                  });

        var service = new DataflowPipelineBackgroundService(
            new[] { clientMock.Object },
            repoMock.Object,
            dedupMock.Object,
            metricsMock.Object,
            loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Should_Increment_Metrics_For_Each_Tick()
    {
        // Arrange
        var repoMock = new Mock<ITickRepository>();
        var dedupMock = new Mock<IDeduplicator>();
        var metricsMock = new Mock<IMetricsService>();
        var loggerMock = new Mock<ILogger<DataflowPipelineBackgroundService>>();

        dedupMock.Setup(d => d.IsDuplicate(It.IsAny<Tick>()))
                 .Returns(false);

        var clientMock = new Mock<IWebSocketClient>();

        clientMock.Setup(c => c.RunAsync(It.IsAny<ITargetBlock<Tick>>(), It.IsAny<CancellationToken>()))
                  .Returns(async (ITargetBlock<Tick> target, CancellationToken ct) =>
                  {
                      await target.SendAsync(new Tick());
                      await target.SendAsync(new Tick());
                      target.Complete();
                  });

        var service = new DataflowPipelineBackgroundService(
            new[] { clientMock.Object },
            repoMock.Object,
            dedupMock.Object,
            metricsMock.Object,
            loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        metricsMock.Verify(m => m.Increment(), Times.AtLeast(2));
    }*/
}