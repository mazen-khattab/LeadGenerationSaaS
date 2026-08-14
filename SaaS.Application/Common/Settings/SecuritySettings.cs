using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Settings
{
    public sealed class SecuritySettings
    {
        public const string SectionName = "SecuritySettings";
        public string EncryptionKey { get; set; } = string.Empty;
        public int PasswordWorkFactor { get; set; } = 12;


        // JWT Configuration
        public string JwtSecret { get; set; } = string.Empty;
        public string JwtIssuer { get; set; } = string.Empty;
        public string JwtAudience { get; set; } = string.Empty;
        public int AccessTokenExpirationMinutes { get; set; } = 15;
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }
}
