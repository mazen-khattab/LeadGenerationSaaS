using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaaS.Domain.Entities;

namespace SaaS.Infrastructure.Persistence.Configurations;

public class RunConfiguration : IEntityTypeConfiguration<Run>
{
    public void Configure(EntityTypeBuilder<Run> builder)
    {
        builder.ToTable("Runs");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .UseIdentityColumn();

        builder.Property(r => r.CollectedLeadsCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(r => r.InfoJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)")
            .HasDefaultValue("{}");

        builder.Property(r => r.StartedAt)
            .IsRequired()
            .HasColumnType("datetime")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(r => r.EndedAt)
            .HasColumnType("datetime")
            .IsRequired(false);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasColumnType("varchar(50)")
            .HasDefaultValue("Running");

        // User is required and Restrict-deleted: a Run must always belong to a user,
        // and that user cannot be hard-deleted while runs still reference them.
        builder.HasOne(r => r.User)
            .WithMany(u => u.Runs)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bot / TargetGroup / ConnectedAccount are optional (nullable FK) and use
        // SetNull on delete: a Run is historical data, so if the Bot/Group/Account it
        // used is later removed, the Run survives with that reference cleared instead
        // of being deleted itself or blocking the parent's deletion.
        builder.HasOne(r => r.Bot)
            .WithMany(b => b.Runs)
            .HasForeignKey(r => r.BotId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(r => r.Group)
            .WithMany(g => g.Runs)
            .HasForeignKey(r => r.GroupId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(r => r.Account)
            .WithMany(a => a.Runs)
            .HasForeignKey(r => r.AccountId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Covers the dashboard-style query: "this user's runs, for this bot, filtered
        // by status" (e.g. list all 'Running' runs for User X on Bot Y).
        builder.HasIndex(r => new { r.UserId, r.BotId, r.Status })
            .HasDatabaseName("IX_Runs_UserId_BotId_Status");
    }
}