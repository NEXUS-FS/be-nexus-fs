using Domain.Entities;
namespace Domain.Repositories;

public interface IAuditLogRepository
{
    Task<AuditLogEntity> AddAsync(AuditLogEntity auditLog);

    /// <summary>
    /// Get audit logs for a specific user since a given date.
    /// </summary>
    Task<IEnumerable<AuditLogEntity>> GetByUserIdAsync(string userId, DateTime since);

    /// <summary>
    /// Get all audit logs in a given date range.
    /// </summary>
    Task<IEnumerable<AuditLogEntity>> GetByDateRangeAsync(DateTime from, DateTime to);

    /// <summary>
    /// Get the most recent N audit logs.
    /// </summary>
    Task<IEnumerable<AuditLogEntity>> GetRecentAsync(int count = 50);
}
