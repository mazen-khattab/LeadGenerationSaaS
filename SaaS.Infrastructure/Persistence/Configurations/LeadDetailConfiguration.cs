using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaaS.Domain.Entities;

namespace SaaS.Infrastructure.Persistence.Configurations;

public class LeadDetailConfiguration : IEntityTypeConfiguration<LeadDetail>
{
    public void Configure(EntityTypeBuilder<LeadDetail> builder)
    {
        // Same physical table as Lead. This single line is what turns the
        // relationship into table SPLITTING instead of an ordinary 1-to-1 with two
        // tables and a real foreign key constraint between them.
        builder.ToTable("Leads");

        builder.HasKey(ld => ld.LeadId);

        builder.Property(ld => ld.MetaDataJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)")
            .HasDefaultValue("{}");

        // No OnDelete/cascade configuration is needed here: since Lead and LeadDetail
        // occupy the same row of the same table, there is no separate row for SQL
        // Server to cascade-delete. Deleting the Lead deletes the LeadDetail columns
        // with it automatically - the "Cascade Delete" from the spec is inherent to
        // table splitting, not something you configure via DeleteBehavior.
    }
}