using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Domain.Entities
{
    public class SystemAdmin
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Admin"; // e.g. "SuperAdmin", "Admin", "Support"
        public bool IsActive { get; set; } = true;
        public string? TwoFactorSecret { get; set; }
        public bool IsTwoFactorEnabled { get; set; } = false;
        public DateTime? LastLoginAt { get; set; }
        public string? LastLoginIp { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        public ICollection<SystemAdminRefreshTokens> RefreshTokens { get; set; } = new List<SystemAdminRefreshTokens>();
    }
}
