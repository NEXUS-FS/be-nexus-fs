using Application.DTOs;
using Domain.Repositories;

namespace Application.UseCases.AuditLogs.Queries.GetAuditLogs
{
    public class GetAuditLogsHandler
    {
        private readonly IAuditLogRepository _repository;

        public GetAuditLogsHandler(IAuditLogRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<LogEntry>> HandleAsync(GetAuditLogsQuery query)
        {
            IEnumerable<Domain.Entities.AuditLogEntity> logs;

            if (!string.IsNullOrEmpty(query.UserId))
            {
                logs = await _repository.GetByUserIdAsync(query.UserId, query.From ?? DateTime.MinValue);
            }
            else if (query.From.HasValue && query.To.HasValue)
            {
                logs = await _repository.GetByDateRangeAsync(query.From.Value, query.To.Value);
            }
            else
            {
                logs = await _repository.GetRecentAsync();
            }

          
            return logs.Select(l => new LogEntry
            {
                Level = string.IsNullOrEmpty(l.Details) ? "Info" : "Error",
                Message = $"{l.Action} on {l.ResourcePath ?? "Unknown"}",
                Source = l.UserId ?? "System",
                Exception = l.Details,
                Timestamp = l.Timestamp
            });
        }
    }
}