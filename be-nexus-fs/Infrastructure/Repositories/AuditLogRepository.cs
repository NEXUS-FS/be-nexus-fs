using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly NexusFSDbContext _context;

    public AuditLogRepository(NexusFSDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<AuditLogEntity> AddAsync(AuditLogEntity auditLog)
    {
        await _context.AuditLogs.AddAsync(auditLog);
        await _context.SaveChangesAsync();
        return auditLog;
    }

    public async Task<IEnumerable<AuditLogEntity>> GetByEntityIdAsync(string entityId, DateTime since)
    {
        return await _context.AuditLogs
            .Where(log => log.UserId == entityId && log.Timestamp >= since)
            .OrderByDescending(log => log.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLogEntity>> GetAllAsync(DateTime since)
    {
        return await _context.AuditLogs
            .Where(log => log.Timestamp >= since)
            .OrderByDescending(log => log.Timestamp)
            .ToListAsync();
    }
}