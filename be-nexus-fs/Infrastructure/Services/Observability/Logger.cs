using Application.DTOs;
using Domain.Repositories;

/// <summary>
/// Observer Pattern implementation for logging.
/// Captures and manages logs for all system components.
/// Implements IProviderObserver to react to provider events.
/// </summary>

namespace Infrastructure.Services.Observability
{
    public class Logger : IProviderObserver
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly List<LogEntry> _logBuffer;

        public Logger(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
            _logBuffer = new List<LogEntry>();
        }

        public void LogInformation(string message, string source)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public void LogWarning(string message, string source)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public void LogError(string message, string source, Exception exception = null)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public void LogDebug(string message, string source)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public async Task FlushLogsAsync()
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        // IProviderObserver implementation
        public async Task OnProviderRegistered(string providerId, string providerType)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public async Task OnProviderRemoved(string providerId)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public async Task OnProviderStatusChanged(string providerId, string status)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public async Task OnProviderError(string providerId, Exception exception)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }
    }
}
