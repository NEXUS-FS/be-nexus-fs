using Application.DTOs;
using Domain.Entities;
using Domain.Repositories;

namespace Infrastructure.Services.Observability;

/// <summary>
/// Observer Pattern implementation for logging.
/// </summary>
public class Logger : IProviderObserver
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly List<LogEntry> _logBuffer;
    private readonly object _bufferLock = new();

    public Logger(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        _logBuffer = new List<LogEntry>();
    }

    #region Logging Methods

    public void LogInformation(string message, string source = "System")
    {
        Log("INFO", message, source, null);
    }

    public void LogWarning(string message, string source = "System")
    {
        Log("WARNING", message, source, null);
    }

    public void LogError(string message, string source = "System", Exception? exception = null)
    {
        Log("ERROR", message, source, exception);
    }

    public void LogDebug(string message, string source = "System")
    {
        Log("DEBUG", message, source, null);
    }

    private void Log(string level, string message, string source, Exception? exception)
    {
        var logEntry = new LogEntry
        {
            Level = level,
            Message = message,
            Source = source,
            Exception = exception?.ToString(),
            Timestamp = DateTime.UtcNow
        };

        lock (_bufferLock)
        {
            _logBuffer.Add(logEntry);
        }

        // Console output for immediate feedback
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var logMessage = $"[{timestamp}] [{level}] [{source}] {message}";
        
        if (exception != null)
            logMessage += $"\nException: {exception.Message}";

        Console.WriteLine(logMessage);
    }

    public async Task FlushLogsAsync()
    {
        List<LogEntry> logsToFlush;

        lock (_bufferLock)
        {
            if (_logBuffer.Count == 0)
                return;

            logsToFlush = new List<LogEntry>(_logBuffer);
            _logBuffer.Clear();
        }

        // Persist logs to database
        foreach (var log in logsToFlush)
        {
            await _auditLogRepository.AddAsync(new AuditLogEntity
            {
                Id = Guid.NewGuid().ToString(),
                Action = log.Level,
                Details = $"{log.Message} | Source: {log.Source} | Exception: {log.Exception}",
                Timestamp = log.Timestamp,
                UserId = "System"
            });
        }

        Console.WriteLine($"[Logger] Flushed {logsToFlush.Count} logs to database");
    }

    #endregion

    #region IProviderObserver Implementation

    public async Task OnProviderRegistered(string providerId, string providerType)
    {
        LogInformation($"Provider registered: {providerId} (Type: {providerType})", "ProviderObserver");
        await Task.CompletedTask;
    }

    public async Task OnProviderRemoved(string providerId)
    {
        LogInformation($"Provider removed: {providerId}", "ProviderObserver");
        await Task.CompletedTask;
    }

    public async Task OnProviderStatusChanged(string providerId, string status)
    {
        LogInformation($"Provider status changed: {providerId} -> {status}", "ProviderObserver");
        await Task.CompletedTask;
    }

    public async Task OnProviderError(string providerId, Exception exception)
    {
        LogError($"Provider error: {providerId}", "ProviderObserver", exception);
        await Task.CompletedTask;
    }

    public async Task TrackEventAsync(string eventName, object data)
    {
        var dataJson = System.Text.Json.JsonSerializer.Serialize(data);
        LogInformation($"Event tracked: {eventName} - Data: {dataJson}", "EventTracker");
        await Task.CompletedTask;
    }

    public async Task<Dictionary<string, object>> GetProviderMetricsAsync(string providerId)
    {
        var providerLogs = _logBuffer
            .Where(l => l.Source?.Contains(providerId) == true)
            .ToList();

        return await Task.FromResult(new Dictionary<string, object>
        {
            { "ProviderId", providerId },
            { "TotalLogs", providerLogs.Count },
            { "ErrorCount", providerLogs.Count(l => l.Level == "ERROR") },
            { "WarningCount", providerLogs.Count(l => l.Level == "WARNING") },
            { "LastUpdated", DateTime.UtcNow }
        });
    }

    #endregion
}