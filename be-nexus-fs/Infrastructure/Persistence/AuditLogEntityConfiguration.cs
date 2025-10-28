using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations;

public class AuditLogEntityConfiguration : IEntityTypeConfiguration<AuditLogEntity>
{
    public void Configure(EntityTypeBuilder<AuditLogEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ResourcePath)
            .HasMaxLength(500);

        builder.Property(e => e.Details)
            .HasColumnType("text");

        builder.Property(e => e.Timestamp)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasIndex(e => e.Action)
            .HasDatabaseName("IX_AuditLogs_Action");
    }
}