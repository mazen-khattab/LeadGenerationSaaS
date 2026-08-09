using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaaS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Infrastructure.Persistence.Configurations
{
    public class SystemAdminRefreshTokensConfiguration : IEntityTypeConfiguration<SystemAdminRefreshTokens>
    {
        public void Configure(EntityTypeBuilder<SystemAdminRefreshTokens> builder)
        {
            builder.ToTable("AdminRefreshTokens");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Token)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasIndex(r => r.Token)
                .IsUnique();

            builder.Property(r => r.ExpDate)
                .IsRequired();

            builder.Property(r => r.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(rt => rt.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasIndex(rt => rt.AdminId)
                .IsUnique()
                .HasFilter("[IsActive] = 1")
                .HasDatabaseName("UX_RefreshTokens_AdminId_Active");

            //builder.HasIndex(rt => rt.SystemAdminId)
            //    .IsUnique()
            //    .HasFilter("[IsActive] = 1")
            //    .HasDatabaseName("UX_RefreshTokens_SystemAdminId_Active");

            // Optional relationship with Admin
            builder.HasOne(r => r.Admin)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.AdminId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            //// Optional relationship with SystemAdmin
            //builder.HasOne(r => r.SystemAdmin)
            //    .WithMany(a => a.UserRefreshTokens)
            //    .HasForeignKey(r => r.SystemAdminId)
            //    .OnDelete(DeleteBehavior.Cascade)
            //    .IsRequired(false);

            //// Ensure a UserRefreshToken belongs to EITHER a User OR a SystemAdmin
            //builder.ToTable(t => t.HasCheckConstraint(
            //    "CK_RefreshToken_Owner",
            //    "(UserId IS NOT NULL AND SystemAdminId IS NULL) OR (UserId IS NULL AND SystemAdminId IS NOT NULL)"
            //));
        }
    }
}