namespace Application.DTOs;

public class MetricsData
{
    public required string MetricName { get; set; }
    public double Value { get; set; }
    public string? Unit { get; set; }
    public string? ProviderId { get; set; }
    public string? ProviderType { get; set; }
    public string? Tags { get; set; }
    public DateTime Timestamp { get; set; }

    // Mapper method
    public static MetricsData FromEntity(Domain.Entities.MetricEntity entity)
    {
        return new MetricsData
        {
            MetricName = entity.MetricName,
            Value = entity.Value,
            Unit = entity.Unit,
            ProviderId = entity.ProviderId,
            ProviderType = entity.ProviderType,
            Tags = entity.Tags,
            Timestamp = entity.Timestamp
        };
    }
}