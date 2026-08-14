using Microsoft.AspNetCore.Http;
using SaaS.Application.Common.Interfaces;
using SaaS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SaaS.Infrastructure.DataSeeding
{
    public static class UsersSeeding
    {
        public static async Task SeedingAsync(IAppDbContext context, IPasswordHasherService hasher, IHttpContextAccessor _httpContext)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(hasher);
            ArgumentNullException.ThrowIfNull(_httpContext);

            if (!context.Users.Any())
            {
                User user = new()
                {
                    Id = Guid.NewGuid(),
                    FullName = "Mazen Khattab",
                    PhoneNumber = "01023839637",
                    Email = "mazenkhtab11@gmail.com",
                    PasswordHash = hasher.HashPassword("Mak.12"),
                    CurrentSessionToken = Guid.NewGuid().ToString("N"),
                    LastLoginIp = _httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? null,
                };

                context.Users.Add(user);
            }

            if (!context.SystmeAdmins.Any())
            {
                SystemAdmin user = new()
                {
                    Id = Guid.NewGuid(),
                    FullName = "Mazen Khattab",
                    Email = "mazenkhtab11@gmail.com",
                    PasswordHash = hasher.HashPassword("Mak.12"),
                    LastLoginIp = _httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? null,
                };

                context.SystmeAdmins.Add(user);
            }

            await context.SaveChangesAsync();
        }
    }
}
