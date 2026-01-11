using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.Repositories;
using FluentAssertions;
using Infrastructure.Services.Observability;
using Moq;

namespace NexusFS.Tests;

public class MetricsCollectorTests
{
    [Fact]
    public async Task FlushMetricsAsync_PersistsBufferedMetrics()
    {
        var stored = new List<MetricEntity>();
        var repoMock = CreateMetricRepositoryMock(stored);
        var collector = CreateCollector(repoMock);

        await collector.RecordMetricAsync("test.metric", 1.5, "ms", "p1", "typeA");
        await collector.RecordMetricAsync("other.metric", 2, providerId: "p2", providerType: "typeB");

        repoMock.Verify(r => r.AddAsync(It.IsAny<MetricEntity>()), Times.Never);

        await collector.FlushMetricsAsync();

        stored.Should().HaveCount(2);
        stored.Should().ContainSingle(m =>
            m.MetricName == "test.metric" &&
            m.Value == 1.5 &&
            m.Unit == "ms" &&
            m.ProviderId == "p1" &&
            m.ProviderType == "typeA");
    }

    [Fact]
    public async Task RecordMetricAsync_AutoFlushesAtBufferLimit()
    {
        var repoMock = new Mock<IMetricRepository>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<MetricEntity>()))
            .ReturnsAsync((MetricEntity metric) => metric);

        var collector = CreateCollector(repoMock);

        for (var i = 0; i < 100; i++)
        {
            await collector.RecordMetricAsync("auto.flush", i);
        }

        repoMock.Verify(r => r.AddAsync(It.IsAny<MetricEntity>()), Times.Exactly(100));

        await collector.FlushMetricsAsync();

        repoMock.Verify(r => r.AddAsync(It.IsAny<MetricEntity>()), Times.Exactly(100));
    }

    [Fact]
    public async Task ConvenienceMethods_RecordExpectedMetricNames()
    {
        var stored = new List<MetricEntity>();
        var repoMock = CreateMetricRepositoryMock(stored);
        var collector = CreateCollector(repoMock);

        await collector.IncrementCounterAsync("files", amount: 2, providerId: "p1", providerType: "local");
        await collector.RecordOperationDurationAsync("copy", TimeSpan.FromMilliseconds(50), providerId: "p1", providerType: "local");

        await collector.FlushMetricsAsync();

        stored.Should().ContainSingle(m =>
            m.MetricName == "counter.files" &&
            m.Unit == "count" &&
            m.Value == 2 &&
            m.ProviderId == "p1" &&
            m.ProviderType == "local");

        stored.Should().ContainSingle(m =>
            m.MetricName == "operation.copy.duration" &&
            m.Unit == "ms" &&
            m.Value == 50 &&
            m.ProviderId == "p1" &&
            m.ProviderType == "local");
    }

    [Fact]
    public async Task GetMetricsAsync_FiltersByProviderAndType()
    {
        var repoMock = new Mock<IMetricRepository>(MockBehavior.Loose);
        var collector = CreateCollector(repoMock);

        await collector.RecordMetricAsync("metric", 1, providerId: "p1", providerType: "typeA");
        await collector.RecordMetricAsync("metric", 2, providerId: "p1", providerType: "typeB");
        await collector.RecordMetricAsync("metric", 3, providerId: "p2", providerType: "typeA");

        var filtered = await collector.GetMetricsAsync(providerId: "p1", providerType: "typeA");

        filtered.Should().HaveCount(1);
        filtered.First().Value.Should().Be(1);
    }

    private static MetricsCollector CreateCollector(Mock<IMetricRepository> repoMock)
    {
        var auditLogRepo = new Mock<IAuditLogRepository>(MockBehavior.Loose);
        var logger = new Logger(auditLogRepo.Object);
        return new MetricsCollector(repoMock.Object, logger);
    }

    private static Mock<IMetricRepository> CreateMetricRepositoryMock(List<MetricEntity> storage)
    {
        var repoMock = new Mock<IMetricRepository>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<MetricEntity>()))
            .ReturnsAsync((MetricEntity metric) =>
            {
                storage.Add(metric);
                return metric;
            });

        return repoMock;
    }
}
