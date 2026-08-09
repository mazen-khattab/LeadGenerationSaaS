using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Domain.Entities
{
    public class UserSetting
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyPitch { get; set; }
        public int DailyMessageLimit { get; set; } = 30;

        // AES-256 Encrypted API Keys
        public string? AIApiKeyEncrypted { get; set; }
        public string? ScraperApiTokenEncrypted { get; set; }

        // Navigation Property
        public User User { get; set; } = null!;
    }
}
