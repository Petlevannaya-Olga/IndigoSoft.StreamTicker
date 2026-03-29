using System.Threading.Tasks.Dataflow;
using FluentAssertions;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using IndigoSoft.StreamTicker.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace IndigoSoft.StreamTicker.Tests.Units;

public class DataflowPipelineTests
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
    public async Task Should_process_ticks_and_save_batches()
    {
        // Arrange
        var repository = new Mock<ITickRepository>();
        var deduplicator = new Mock<IDeduplicator>();
        var metrics = new Mock<IMetricsService>();
        var logger = new Mock<ILogger<DataflowPipeline>>();

        deduplicator.Setup(d => d.IsDuplicate(It.IsAny<Tick>()))
            .Returns(false);

        var savedBatches = new List<int>();

        repository
            .Setup(r => r.SaveBatchAsync(
                It.IsAny<Tick[]>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Callback<Tick[], int, CancellationToken>((_, count, _) =>
            {
                savedBatches.Add(count);
            })
            .Returns(Task.CompletedTask);

        var client = new Mock<IWebSocketClient<ITargetBlock<Tick>>>();

        client.Setup(c => c.RunAsync(
                It.IsAny<ITargetBlock<Tick>>(),
                It.IsAny<CancellationToken>()))
            .Returns((ITargetBlock<Tick> target, CancellationToken ct) =>
            {
                target.Post(CreateTick());
                target.Post(CreateTick());

                target.Complete();

                return Task.CompletedTask;
            });

        var pipeline = new DataflowPipeline(
            [client.Object],
            repository.Object,
            deduplicator.Object,
            metrics.Object,
            logger.Object);

        // Act
        await pipeline.RunAsync(CancellationToken.None);

        // Assert

        savedBatches.Should().NotBeEmpty();

        savedBatches.Sum().Should().Be(2);

        repository.Verify(r => r.SaveBatchAsync(
                It.IsAny<Tick[]>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Should_filter_duplicates()
    {
        var repository = new Mock<ITickRepository>();
        var deduplicator = new Mock<IDeduplicator>();
        var metrics = new Mock<IMetricsService>();
        var logger = new Mock<ILogger<DataflowPipeline>>();

        deduplicator.Setup(d => d.IsDuplicate(It.IsAny<Tick>()))
            .Returns(true);

        var client = new Mock<IWebSocketClient<ITargetBlock<Tick>>>();

        client.Setup(c => c.RunAsync(It.IsAny<ITargetBlock<Tick>>(), It.IsAny<CancellationToken>()))
            .Returns((ITargetBlock<Tick> target, CancellationToken ct) =>
            {
                target.Post(CreateTick());
                // target.Complete();
                return Task.CompletedTask;
            });

        var pipeline = new DataflowPipeline(
            [client.Object],
            repository.Object,
            deduplicator.Object,
            metrics.Object,
            logger.Object);

        // Act
        await pipeline.RunAsync(CancellationToken.None);

        // Assert
        repository.Verify(r => r.SaveBatchAsync(
                It.IsAny<Tick[]>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_increment_metrics()
    {
        var repository = new Mock<ITickRepository>();
        var deduplicator = new Mock<IDeduplicator>();
        var metrics = new Mock<IMetricsService>();
        var logger = new Mock<ILogger<DataflowPipeline>>();

        deduplicator.Setup(d => d.IsDuplicate(It.IsAny<Tick>()))
            .Returns(false);

        var client = new Mock<IWebSocketClient<ITargetBlock<Tick>>>();

        client.Setup(c => c.RunAsync(It.IsAny<ITargetBlock<Tick>>(), It.IsAny<CancellationToken>()))
            .Returns((ITargetBlock<Tick> target, CancellationToken ct) =>
            {
                target.Post(CreateTick());
                // target.Complete();
                return Task.CompletedTask;
            });

        var pipeline = new DataflowPipeline(
            [client.Object],
            repository.Object,
            deduplicator.Object,
            metrics.Object,
            logger.Object);

        // Act
        await pipeline.RunAsync(CancellationToken.None);

        // Assert
        metrics.Verify(m => m.IncrementIn(), Times.AtLeastOnce);
        metrics.Verify(m => m.IncrementOut(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Should_complete_pipeline()
    {
        var repository = new Mock<ITickRepository>();
        var deduplicator = new Mock<IDeduplicator>();
        var metrics = new Mock<IMetricsService>();
        var logger = new Mock<ILogger<DataflowPipeline>>();

        deduplicator.Setup(d => d.IsDuplicate(It.IsAny<Tick>()))
            .Returns(false);

        repository
            .Setup(r => r.SaveBatchAsync(It.IsAny<Tick[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = new Mock<IWebSocketClient<ITargetBlock<Tick>>>();

        client.Setup(c => c.RunAsync(It.IsAny<ITargetBlock<Tick>>(), It.IsAny<CancellationToken>()))
            .Returns((ITargetBlock<Tick> target, CancellationToken ct) =>
            {
                target.Post(CreateTick());
                // target.Complete();
                return Task.CompletedTask;
            });

        var pipeline = new DataflowPipeline(
            [client.Object],
            repository.Object,
            deduplicator.Object,
            metrics.Object,
            logger.Object);

        var task = pipeline.RunAsync(CancellationToken.None);

        var completed = await Task.WhenAny(task, Task.Delay(1000));

        Assert.Equal(task, completed);
    }

    [Fact]
    public async Task Should_create_batches()
    {
        // Arrange
        var repository = new Mock<ITickRepository>();
        var deduplicator = new Mock<IDeduplicator>();
        var metrics = new Mock<IMetricsService>();
        var logger = new Mock<ILogger<DataflowPipeline>>();

        deduplicator.Setup(d => d.IsDuplicate(It.IsAny<Tick>()))
            .Returns(false);

        var savedBatchSizes = new List<int>();

        repository
            .Setup(r => r.SaveBatchAsync(
                It.IsAny<Tick[]>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Callback<Tick[], int, CancellationToken>((_, count, _) =>
            {
                savedBatchSizes.Add(count);
            })
            .Returns(Task.CompletedTask);

        var client = new Mock<IWebSocketClient<ITargetBlock<Tick>>>();

        client.Setup(c => c.RunAsync(It.IsAny<ITargetBlock<Tick>>(), It.IsAny<CancellationToken>()))
            .Returns((ITargetBlock<Tick> target, CancellationToken ct) =>
            {
                for (var i = 0; i < 5000; i++)
                {
                    target.Post(CreateTick());
                }

                return Task.CompletedTask;
            });

        var pipeline = new DataflowPipeline(
            [client.Object],
            repository.Object,
            deduplicator.Object,
            metrics.Object,
            logger.Object);

        // Act
        await pipeline.RunAsync(CancellationToken.None);

        // Assert

        savedBatchSizes.Should().NotBeEmpty();

        savedBatchSizes.Should().AllSatisfy(size =>
            size.Should().BeInRange(1, 2000));

        savedBatchSizes.Sum().Should().Be(5000);
    }

    [Fact]
    public async Task Should_handle_multiple_clients()
    {
        // Arrange
        var repository = new Mock<ITickRepository>();
        var deduplicator = new Mock<IDeduplicator>();
        var metrics = new Mock<IMetricsService>();
        var logger = new Mock<ILogger<DataflowPipeline>>();

        deduplicator.Setup(d => d.IsDuplicate(It.IsAny<Tick>()))
            .Returns(false);

        var startCounter = 0;
        var savedCount = 0;

        repository
            .Setup(r => r.SaveBatchAsync(
                It.IsAny<Tick[]>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref savedCount))
            .Returns(Task.CompletedTask);

        var clients = Enumerable.Range(0, 3)
            .Select(_ =>
            {
                var client = new Mock<IWebSocketClient<ITargetBlock<Tick>>>();

                client.Setup(c => c.RunAsync(
                        It.IsAny<ITargetBlock<Tick>>(),
                        It.IsAny<CancellationToken>()))
                    .Returns((ITargetBlock<Tick> target, CancellationToken ct) =>
                    {
                        Interlocked.Increment(ref startCounter);

                        target.Post(CreateTick());

                        return Task.CompletedTask;
                    });

                return client.Object;
            })
            .ToArray();

        var pipeline = new DataflowPipeline(
            clients,
            repository.Object,
            deduplicator.Object,
            metrics.Object,
            logger.Object);

        // Act
        await pipeline.RunAsync(CancellationToken.None);

        // Assert

        // Все клиенты стартовали
        startCounter.Should().Be(3);

        // Хотя бы один батч сохранился
        savedCount.Should().BeGreaterThan(0);

        // Проверка вызова репозитория
        repository.Verify(r => r.SaveBatchAsync(
                It.IsAny<Tick[]>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
    
    [Fact]
    public async Task Should_count_deduplicated_ticks()
    {
        var repository = new Mock<ITickRepository>();
        var deduplicator = new Mock<IDeduplicator>();
        var metrics = new Mock<IMetricsService>();
        var logger = new Mock<ILogger<DataflowPipeline>>();

        deduplicator.Setup(d => d.IsDuplicate(It.IsAny<Tick>()))
            .Returns(true);

        var client = new Mock<IWebSocketClient<ITargetBlock<Tick>>>();

        client.Setup(c => c.RunAsync(It.IsAny<ITargetBlock<Tick>>(), It.IsAny<CancellationToken>()))
            .Returns((ITargetBlock<Tick> target, CancellationToken ct) =>
            {
                target.Post(CreateTick());
                //target.Complete();
                return Task.CompletedTask;
            });

        var pipeline = new DataflowPipeline(
            [client.Object],
            repository.Object,
            deduplicator.Object,
            metrics.Object,
            logger.Object);

        await pipeline.RunAsync(CancellationToken.None);

        metrics.Verify(m => m.IncrementDeduplicated(), Times.Once);
        metrics.Verify(m => m.IncrementOut(), Times.Never);
    }
    
    [Fact]
    public async Task Should_not_throw_when_repository_fails()
    {
        var repository = new Mock<ITickRepository>();
        var deduplicator = new Mock<IDeduplicator>();
        var metrics = new Mock<IMetricsService>();
        var logger = new Mock<ILogger<DataflowPipeline>>();

        deduplicator.Setup(d => d.IsDuplicate(It.IsAny<Tick>()))
            .Returns(false);

        repository
            .Setup(r => r.SaveBatchAsync(It.IsAny<Tick[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB fail"));

        var client = new Mock<IWebSocketClient<ITargetBlock<Tick>>>();

        client.Setup(c => c.RunAsync(It.IsAny<ITargetBlock<Tick>>(), It.IsAny<CancellationToken>()))
            .Returns((ITargetBlock<Tick> target, CancellationToken ct) =>
            {
                target.Post(CreateTick());
                //target.Complete();
                return Task.CompletedTask;
            });

        var pipeline = new DataflowPipeline(
            [client.Object],
            repository.Object,
            deduplicator.Object,
            metrics.Object,
            logger.Object);

        var act = () => pipeline.RunAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task Should_run_clients_in_parallel()
    {
        var repository = new Mock<ITickRepository>();
        var deduplicator = new Mock<IDeduplicator>();
        var metrics = new Mock<IMetricsService>();
        var logger = new Mock<ILogger<DataflowPipeline>>();

        deduplicator.Setup(d => d.IsDuplicate(It.IsAny<Tick>()))
            .Returns(false);

        repository
            .Setup(r => r.SaveBatchAsync(It.IsAny<Tick[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var started = 0;
        var maxConcurrent = 0;
        var current = 0;

        var clients = Enumerable.Range(0, 3)
            .Select(i =>
            {
                var client = new Mock<IWebSocketClient<ITargetBlock<Tick>>>();

                client.Setup(c => c.RunAsync(It.IsAny<ITargetBlock<Tick>>(), It.IsAny<CancellationToken>()))
                    .Returns(async (ITargetBlock<Tick> target, CancellationToken ct) =>
                    {
                        Interlocked.Increment(ref started);

                        var now = Interlocked.Increment(ref current);
                        Interlocked.Exchange(ref maxConcurrent, Math.Max(maxConcurrent, now));

                        await Task.Delay(100, ct);

                        Interlocked.Decrement(ref current);

                        target.Post(new Tick("Exchange", "AAPL", 100, 10, 1));
                        //target.Complete();
                    });

                return client.Object;
            })
            .ToArray();

        var pipeline = new DataflowPipeline(
            clients,
            repository.Object,
            deduplicator.Object,
            metrics.Object,
            logger.Object);

        await pipeline.RunAsync(CancellationToken.None);

        started.Should().Be(3);

        // ключевая проверка
        maxConcurrent.Should().BeGreaterThan(1);
    }
}