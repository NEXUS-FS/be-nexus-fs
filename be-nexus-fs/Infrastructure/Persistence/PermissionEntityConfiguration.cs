using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations;

public class PermissionEntityConfiguration : IEntityTypeConfiguration<PermissionEntity>
{
    public void Configure(EntityTypeBuilder<PermissionEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Permission)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.GrantedAt)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(e => new { e.Username, e.Permission })
            .IsUnique()
            .HasDatabaseName("IX_Permissions_Username_Permission");

        builder.HasIndex(e => e.Username)
            .HasDatabaseName("IX_Permissions_Username");
    }
}
