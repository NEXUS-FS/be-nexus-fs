using System;
using System.Threading.Tasks;
using FluentAssertions;
using Infrastructure.Services.Observability;
using Moq;
using Domain.Repositories;
using Xunit;

namespace NexusFS.Tests
{
    public class LoggerTests
    {
        [Fact]
        public async Task FlushLogsAsync_PersistsBufferedLogs()
        {
            var auditRepo = new Mock<IAuditLogRepository>();
            auditRepo.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.AuditLogEntity>()))
                .ReturnsAsync((Domain.Entities.AuditLogEntity a) => a ?? new Domain.Entities.AuditLogEntity { Id = Guid.NewGuid().ToString(), Action = "INFO", Details = "test", Timestamp = DateTime.UtcNow, UserId = "System" });

            var logger = new Logger(auditRepo.Object);

            logger.LogInformation("info-msg", "UnitTest");
            logger.LogWarning("warn-msg", "UnitTest");
            logger.LogError("err-msg", "UnitTest", new System.Exception("boom"));

            await logger.FlushLogsAsync();

            auditRepo.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.AuditLogEntity>()), Times.Exactly(3));
        }

        [Fact]
        public async Task GetProviderMetricsAsync_ReturnsMetrics()
        {
            var auditRepo = new Mock<IAuditLogRepository>();
            var logger = new Logger(auditRepo.Object);

            logger.LogInformation("Provider registered: p1", "ProviderObserver");
            logger.LogError("Provider error: p1", "ProviderObserver", new System.Exception("x"));

            var metrics = await logger.GetProviderMetricsAsync("p1");
            metrics.Should().ContainKey("ProviderId");
            metrics.Should().ContainKey("TotalLogs");
        }
    }
}
