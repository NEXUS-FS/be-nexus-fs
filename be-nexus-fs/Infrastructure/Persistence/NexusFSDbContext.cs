// Infrastructure/Persistence/NexusFSDbContext.cs
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Infrastructure.Persistence;

/// <summary>
/// Database context for NexusFS application.
/// Manages entity configurations and database operations.
/// </summary>
public class NexusFSDbContext : DbContext
{
    public NexusFSDbContext(DbContextOptions<NexusFSDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProviderEntity> Providers { get; set; }
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<AccessControlEntity> AccessControls { get; set; }
    public DbSet<AuditLogEntity> AuditLogs { get; set; }
    public DbSet<MetricEntity> Metrics { get; set; }

    //protected override void OnModelCreating(ModelBuilder modelBuilder)
    //{
    //    base.OnModelCreating(modelBuilder);

    //    // ProviderEntity configuration
    //    modelBuilder.Entity<ProviderEntity>(entity =>
    //    {
    //        entity.HasKey(e => e.Id);
    //        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
    //        entity.Property(e => e.ProviderType).IsRequired().HasMaxLength(50);
    //        entity.Property(e => e.Configuration).HasColumnType("nvarchar(max)");
    //        entity.HasIndex(e => e.Name).IsUnique();
    //    });

    //    // UserEntity configuration
    //    modelBuilder.Entity<UserEntity>(entity =>
    //    {
    //        entity.HasKey(e => e.Id);
    //        entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
    //        entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
    //        entity.HasIndex(e => e.Username).IsUnique();
    //        entity.HasIndex(e => e.Email).IsUnique();
    //    });

    //    // AccessControlEntity configuration
    //    modelBuilder.Entity<AccessControlEntity>(entity =>
    //    {
    //        entity.HasKey(e => e.Id);
    //        entity.Property(e => e.ResourcePath).IsRequired().HasMaxLength(500);
    //        entity.Property(e => e.Permissions).HasColumnType("nvarchar(max)");

    //        entity.HasOne(e => e.User)
    //            .WithMany()
    //            .HasForeignKey(e => e.UserId)
    //            .OnDelete(DeleteBehavior.Cascade);

    //        entity.HasIndex(e => new { e.UserId, e.ResourcePath });
    //    });

    //    // AuditLogEntity configuration
    //    modelBuilder.Entity<AuditLogEntity>(entity =>
    //    {
    //        entity.HasKey(e => e.Id);
    //        entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
    //        entity.Property(e => e.ResourcePath).HasMaxLength(500);
    //        entity.HasIndex(e => e.Timestamp);
    //        entity.HasIndex(e => e.UserId);
    //    });

    //    // MetricEntity configuration
    //    modelBuilder.Entity<MetricEntity>(entity =>
    //    {
    //        entity.HasKey(e => e.Id);
    //        entity.Property(e => e.MetricName).IsRequired().HasMaxLength(100);
    //        entity.Property(e => e.Tags).HasColumnType("nvarchar(max)");
    //        entity.HasIndex(e => new { e.MetricName, e.Timestamp });
    //    });
    //}
}