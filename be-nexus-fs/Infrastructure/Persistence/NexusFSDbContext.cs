using Domain.Entities;
using Infrastructure.Services.Observability;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;

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
        modelBuilder.Entity<ProviderEntity>().HasQueryFilter(e => e.DeletedAt == null); //needed for soft delete
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = new List<AuditLogEntity>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLogEntity)
                continue;

            // only track Added, Modified, Deleted entities
            if (entry.State == EntityState.Added ||
                entry.State == EntityState.Modified ||
                entry.State == EntityState.Deleted)
            {
                var action = entry.State.ToString(); // "Added", "Modified", "Deleted"
                var resource = entry.Entity.GetType().Name;
                var userId = "SYSTEM"; //TODO actual user id here 
                string? details = null;

                try
                {
                  
                    details = JsonSerializer.Serialize(entry.CurrentValues.ToObject());
                }
               catch (Exception ex)
                {
                    // Log serialization error in Details
                    details = $"{{ \"SerializationError\": \"{ex.Message}\" }}";
                }

                auditEntries.Add(new AuditLogEntity
                {
                    Action = action,
                    ResourcePath = resource,
                    UserId = userId,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // save original changes first
        var result = await base.SaveChangesAsync(cancellationToken);

        // save audit logs if any
        if (auditEntries.Any())
        {
            AuditLogs.AddRange(auditEntries);
            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }
}

