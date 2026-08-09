using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaaS.Domain.Entities;

namespace SaaS.Infrastructure.Persistence.Configurations;

public class SystemAdminConfiguration : IEntityTypeConfiguration<SystemAdmin>
{
    public void Configure(EntityTypeBuilder<SystemAdmin> builder)
    {
        builder.ToTable("SystemAdmins");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(256);

        // Unique Index for Email
        builder.HasIndex(a => a.Email)
            .IsUnique();

        builder.Property(a => a.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Role)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Admin");

        builder.Property(a => a.IsActive)
            .HasDefaultValue(true);

        builder.Property(a => a.TwoFactorSecret)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(a => a.IsTwoFactorEnabled)
            .HasDefaultValue(false);

        builder.Property(a => a.LastLoginIp)
            .HasMaxLength(45) // Supports IPv6 addresses
            .IsRequired(false);

        builder.Property(a => a.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // One-to-Many Relationship with AdminRefreshTokens
        builder.HasMany(a => a.RefreshTokens)
            .WithOne(r => r.Admin)
            .HasForeignKey(r => r.AdminId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}