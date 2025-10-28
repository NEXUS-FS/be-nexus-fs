using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations;

public class MetricEntityConfiguration : IEntityTypeConfiguration<MetricEntity>
{
    public void Configure(EntityTypeBuilder<MetricEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.MetricName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Unit)
            .HasMaxLength(50);

        builder.Property(e => e.ProviderId)
            .HasMaxLength(100);

        builder.Property(e => e.ProviderType)
            .HasMaxLength(50);

        builder.Property(e => e.Tags)
            .HasColumnType("text");

        builder.Property(e => e.Timestamp)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(e => new { e.MetricName, e.Timestamp })
            .HasDatabaseName("IX_Metrics_MetricName_Timestamp");

        builder.HasIndex(e => e.ProviderId)
            .HasDatabaseName("IX_Metrics_ProviderId");
    }
}