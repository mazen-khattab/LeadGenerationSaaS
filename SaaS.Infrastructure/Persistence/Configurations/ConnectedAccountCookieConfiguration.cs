using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaaS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Infrastructure.Persistence.Configurations
{
    public class ConnectedAccountCookieConfiguration : IEntityTypeConfiguration<ConnectedAccountCookie>
    {
        public void Configure(EntityTypeBuilder<ConnectedAccountCookie> builder)
        {
            builder.ToTable("ConnectedAccountCookies");

            builder.HasKey(t => t.AccountId);

            builder.Property(t => t.EncryptedCookies)
                .IsRequired();

            builder.Property(ca => ca.CookiesExpireDate)
                .IsRequired()
                .HasColumnType("datetime");
        }
    }
}
