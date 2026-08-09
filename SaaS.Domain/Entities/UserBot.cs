using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Domain.Entities
{
    public class UserBot
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int BotId { get; set; }
        public decimal PurchasePrice { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public User User { get; set; } = null!;
        public Bot Bot { get; set; } = null!;
    }
}
