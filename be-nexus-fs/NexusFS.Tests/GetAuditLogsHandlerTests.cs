using Application.UseCases.AuditLogs.Queries.GetAuditLogs;
using Domain.Entities;
using Domain.Repositories;
using FluentAssertions;
using Moq;

namespace NexusFS.Tests;

public class GetAuditLogsHandlerTests
{
    [Fact]
    public async Task HandleAsync_UsesUserQuery_WhenUserIdProvided()
    {
        var log = new AuditLogEntity
        {
            Action = "Created",
            ResourcePath = "/file.txt",
            UserId = "user-1",
            Details = string.Empty,
            Timestamp = DateTime.UtcNow
        };

        var repo = new Mock<IAuditLogRepository>();
        repo.Setup(r => r.GetByUserIdAsync("user-1", DateTime.MinValue))
            .ReturnsAsync(new List<AuditLogEntity> { log });

        var handler = new GetAuditLogsHandler(repo.Object);

        var result = (await handler.HandleAsync(new GetAuditLogsQuery { UserId = "user-1" })).ToList();

        repo.Verify(r => r.GetByUserIdAsync("user-1", DateTime.MinValue), Times.Once);
        result.Should().ContainSingle();
        result[0].Level.Should().Be("Info");
        result[0].Message.Should().Be("Created on /file.txt");
        result[0].Source.Should().Be("user-1");
    }

    [Fact]
    public async Task HandleAsync_UsesDateRange_WhenFromAndToProvided()
    {
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow;
        var log = new AuditLogEntity
        {
            Action = "Delete",
            ResourcePath = null,
            UserId = null,
            Details = "failure",
            Timestamp = DateTime.UtcNow
        };

        var repo = new Mock<IAuditLogRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(from, to))
            .ReturnsAsync(new List<AuditLogEntity> { log });

        var handler = new GetAuditLogsHandler(repo.Object);

        var result = (await handler.HandleAsync(new GetAuditLogsQuery { From = from, To = to })).ToList();

        repo.Verify(r => r.GetByDateRangeAsync(from, to), Times.Once);
        result.Should().ContainSingle();
        result[0].Level.Should().Be("Error");
        result[0].Message.Should().Be("Delete on Unknown");
        result[0].Source.Should().Be("System");
        result[0].Exception.Should().Be("failure");
    }

    [Fact]
    public async Task HandleAsync_DefaultsToRecent_WhenNoFiltersProvided()
    {
        var log = new AuditLogEntity
        {
            Action = "Viewed",
            ResourcePath = "/index",
            UserId = "user-2",
            Details = null,
            Timestamp = DateTime.UtcNow
        };

        var repo = new Mock<IAuditLogRepository>();
        repo.Setup(r => r.GetRecentAsync(It.IsAny<int>())).ReturnsAsync(new List<AuditLogEntity> { log });

        var handler = new GetAuditLogsHandler(repo.Object);

        var result = (await handler.HandleAsync(new GetAuditLogsQuery())).ToList();

        repo.Verify(r => r.GetRecentAsync(It.IsAny<int>()), Times.Once);
        result.Should().ContainSingle();
        result[0].Message.Should().Be("Viewed on /index");
    }
}
