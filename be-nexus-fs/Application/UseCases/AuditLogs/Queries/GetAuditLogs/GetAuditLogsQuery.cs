namespace Application.UseCases.AuditLogs.Queries.GetAuditLogs
{
    public class GetAuditLogsQuery
    {
        public string? UserId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}