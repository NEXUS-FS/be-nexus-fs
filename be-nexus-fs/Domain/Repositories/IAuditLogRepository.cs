using Domain.Entities;

namespace Domain.Repositories;

public interface IAuditLogRepository
{
    Task<AuditLogEntity> AddAsync(AuditLogEntity auditLog);
    Task<IEnumerable<AuditLogEntity>> GetByEntityIdAsync(string entityId, DateTime since);
    Task<IEnumerable<AuditLogEntity>> GetAllAsync(DateTime since);
}
