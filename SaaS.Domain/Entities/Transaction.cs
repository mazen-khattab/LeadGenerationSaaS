using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Domain.Entities
{
    public class Transaction
    {
        public int Id { get; set; }
        public Guid UserId { get; set; } // Foreign Key with NO CASCADE to keep audit logs
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        // Navigation Property
        public User User { get; set; } = null!;
    }
}
