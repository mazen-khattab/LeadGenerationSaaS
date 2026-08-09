using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaaS.Domain.Entities;

namespace SaaS.Infrastructure.Persistence.Configurations;

public class TargetGroupConfiguration : IEntityTypeConfiguration<TargetGroup>
{
    public void Configure(EntityTypeBuilder<TargetGroup> builder)
    {
        builder.ToTable("TargetGroups");

        builder.HasKey(tg => tg.Id);

        builder.Property(tg => tg.Id)
            .UseIdentityColumn();

        builder.Property(tg => tg.GroupName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(tg => tg.GroupUrl)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(tg => tg.ConfigJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)")
            .HasDefaultValue("{}");

        builder.Property(tg => tg.LastCursor)
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(tg => tg.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(tg => tg.User)
            .WithMany(u => u.TargetGroups)
            .HasForeignKey(tg => tg.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(tg => tg.Bot)
            .WithMany(b => b.TargetGroups)
            .HasForeignKey(tg => tg.BotId)
            .OnDelete(DeleteBehavior.Restrict);

        //builder.HasIndex(tg => tg.UserId)
        //    .HasDatabaseName("IX_TargetGroups_UserId");

        //builder.HasIndex(tg => tg.BotId)
        //    .HasDatabaseName("IX_TargetGroups_BotId");
    }
}