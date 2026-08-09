using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Domain.Entities
{
    public class SystemAdminRefreshTokens
    {
        public int Id { get; set; }
        public Guid? AdminId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public SystemAdmin? Admin { get; set; }
    }
}
