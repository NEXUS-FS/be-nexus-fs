namespace Application.DTOs;

public class LogEntry
{
    public required string Level { get; set; }
    public required string Message { get; set; }
    public required string Source { get; set; }
    public string? Exception { get; set; }
    public DateTime Timestamp { get; set; }
}