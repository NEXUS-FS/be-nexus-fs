using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations;

public class AccessControlEntityConfiguration : IEntityTypeConfiguration<AccessControlEntity>
{
    public void Configure(EntityTypeBuilder<AccessControlEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ResourcePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Permissions)
            .HasColumnType("text");

        // Foreign Key (if User relationship exists)
        // builder.HasOne(e => e.User)
        //     .WithMany()
        //     .HasForeignKey(e => e.UserId)
        //     .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.ResourcePath })
            .HasDatabaseName("IX_AccessControls_UserId_ResourcePath");
    }
}