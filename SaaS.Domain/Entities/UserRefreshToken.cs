using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Domain.Entities
{
    public class UserRefreshToken
    {
        public int Id { get; set; }
        public Guid? UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public User? User { get; set; }

        //public Guid? SystemAdminId { get; set; }
        //public SystemAdmin? SystemAdmin { get; set; }
    }
}