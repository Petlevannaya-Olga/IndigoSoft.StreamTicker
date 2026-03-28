using FluentAssertions;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using IndigoSoft.StreamTicker.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace IndigoSoft.StreamTicker.Tests.Integrations;

public class PipelineIntegrationsTests
{
    public class PipelineIntegrationTests(TestFixture fixture) : TestBase(fixture)
    {
        [Fact]
        public async Task Should_process_ticks_and_save_to_database()
        {
            // Arrange
            var repository = Get<ITickRepository>();
            var deduplicator = Get<IDeduplicator>();
            var metrics = Get<IMetricsService>();
            var logger = Get<ILogger<Pipeline>>();

            var client = new TestWebSocketClient(100);

            var pipeline = new Pipeline(
                [client],
                repository,
                deduplicator,
                metrics,
                logger);

            // Act
            var task = pipeline.RunAsync(CancellationToken.None);

            var completed = await Task.WhenAny(task, Task.Delay(5000));

            completed.Should().Be(task);

            await task;

            // Assert
            var db = Get<Infrastructure.TickDbContext>();

            db.Ticks.Should().HaveCountGreaterThan(0);

            var values = metrics.GetAndReset();
            values.In.Should().BeGreaterThan(0);
            values.Out.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Should_create_batches_with_correct_size()
        {
            // Arrange
            var repository = new Mock<ITickRepository>();

            var deduplicator = Get<IDeduplicator>();
            var metrics = Get<IMetricsService>();
            var logger = Get<ILogger<Pipeline>>();

            repository
                .Setup(r => r.SaveBatchAsync(It.IsAny<Tick[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var client = new TestWebSocketClient(5000);

            var pipeline = new Pipeline(
                [client],
                repository.Object,
                deduplicator,
                metrics,
                logger);

            // Act
            var task = pipeline.RunAsync(CancellationToken.None);

            var completed = await Task.WhenAny(task, Task.Delay(5000));

            completed.Should().Be(task);

            await task;

            // Assert

            repository.Verify(r => r.SaveBatchAsync(
                    It.Is<Tick[]>(batch => batch.Length <= 2000 && batch.Length > 0),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task Should_remove_duplicates()
        {
            var repository = Get<ITickRepository>();
            var deduplicator = Get<IDeduplicator>();
            var metrics = Get<IMetricsService>();
            var logger = Get<ILogger<Pipeline>>();

            var client = new DuplicateClient(); // отправляет одинаковые ticks

            var pipeline = new Pipeline(
                [client],
                repository,
                deduplicator,
                metrics,
                logger);

            var task = pipeline.RunAsync(CancellationToken.None);

            var completed = await Task.WhenAny(task, Task.Delay(5000));

            completed.Should().Be(task);

            await task;

            var db = Get<Infrastructure.TickDbContext>();

            // гарантирует отсутствие дублей
            db.Ticks
                .GroupBy(t => new { t.Symbol, t.Price, t.Volume, t.EventTime })
                .ToList()
                .Should().OnlyContain(g => g.Count() == 1);
            
            metrics.GetAndReset().Deduplicated.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Should_handle_multiple_clients_with_load()
        {
            var clients = Enumerable.Range(0, 5)
                .Select(_ => new TestWebSocketClient(20_000))
                .ToArray();

            var pipeline = new Pipeline(
                clients,
                Get<ITickRepository>(),
                Get<IDeduplicator>(),
                Get<IMetricsService>(),
                Get<ILogger<Pipeline>>());

            var task = pipeline.RunAsync(CancellationToken.None);

            var completed = await Task.WhenAny(task, Task.Delay(5000));

            completed.Should().Be(task);

            await task;

            var db = Get<Infrastructure.TickDbContext>();

            db.Ticks.Should().HaveCountGreaterThan(1000);
        }

        [Fact]
        public async Task Should_complete_gracefully()
        {
            var pipeline = new Pipeline(
                [new TestWebSocketClient(1000)],
                Get<ITickRepository>(),
                Get<IDeduplicator>(),
                Get<IMetricsService>(),
                Get<ILogger<Pipeline>>());

            var task = pipeline.RunAsync(CancellationToken.None);

            var completed = await Task.WhenAny(task, Task.Delay(5000));

            completed.Should().Be(task);

            await task;
        }
        
        [Fact]
        public async Task Should_deduplicate_across_clients()
        {
            var client1 = new DuplicateClient();
            var client2 = new DuplicateClient();

            var pipeline = new Pipeline(
                [client1, client2],
                Get<ITickRepository>(),
                Get<IDeduplicator>(),
                Get<IMetricsService>(),
                Get<ILogger<Pipeline>>());

            var task = pipeline.RunAsync(CancellationToken.None);

            var completed = await Task.WhenAny(task, Task.Delay(5000));

            completed.Should().Be(task);

            await task;

            var db = Get<Infrastructure.TickDbContext>();

            db.Ticks
                .GroupBy(t => new { t.Symbol, t.Price, t.Volume, t.EventTime })
                .ToList()
                .Should()
                .OnlyContain(g => g.Count() == 1);// должно дедупиться
        }
    }
}