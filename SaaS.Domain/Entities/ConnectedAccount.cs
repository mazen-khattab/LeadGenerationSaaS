using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Domain.Entities
{
    public class ConnectedAccount
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int BotId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public ConnectedAccountCookie Cookie { get; set; } = null!;
        public User User { get; set; } = null!;
        public Bot Bot { get; set; } = null!;
        public ICollection<Run> Runs { get; set; } = new List<Run>();
        public ICollection<Lead> Leads { get; set; } = new List<Lead>();
    }
}
