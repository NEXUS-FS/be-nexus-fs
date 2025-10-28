using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.Persistence;

public class NexusFSDbContext : DbContext
{
    public NexusFSDbContext(DbContextOptions<NexusFSDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProviderEntity> Providers { get; set; } = null!;
    public DbSet<UserEntity> Users { get; set; } = null!;
    public DbSet<AccessControlEntity> AccessControls { get; set; } = null!;
    public DbSet<AuditLogEntity> AuditLogs { get; set; } = null!;
    public DbSet<MetricEntity> Metrics { get; set; } = null!;
    public DbSet<PermissionEntity> Permissions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically apply all IEntityTypeConfiguration from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
