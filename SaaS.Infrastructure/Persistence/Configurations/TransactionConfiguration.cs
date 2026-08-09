using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaaS.Domain.Entities;

namespace SaaS.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .UseIdentityColumn();

        builder.Property(t => t.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(t => t.Currency)
            .IsRequired()
            .HasColumnType("varchar(10)")
            .HasDefaultValue("USD");

        builder.Property(t => t.PaymentMethod)
            .IsRequired()
            .HasColumnType("varchar(50)");

        builder.Property(t => t.TransactionDate)
            .IsRequired()
            .HasColumnType("datetime")
            .HasDefaultValueSql("GETUTCDATE()");

        // Restrict, not Cascade: financial/audit records must never disappear as a
        // side effect of deleting a User. A User with transaction history should be
        // soft-deleted (the User.IsDeleted flag) rather than hard-deleted.
        builder.HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        //builder.HasIndex(t => t.UserId)
        //    .HasDatabaseName("IX_Transactions_UserId");
    }
}