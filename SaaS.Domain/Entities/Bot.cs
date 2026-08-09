using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Domain.Entities
{
    public class Bot
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal CurrentPrice { get; set; }
        public string UiModuleCode { get; set; } = string.Empty;

        // Anti-Spam & Rate Limiting (Default 5 Hours = 300 Mins)
        public int CooldownMinutes { get; set; } = 300;
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public ICollection<UserBot> UserBots { get; set; } = new List<UserBot>();
        public ICollection<ConnectedAccount> ConnectedAccounts { get; set; } = new List<ConnectedAccount>();
        public ICollection<TargetGroup> TargetGroups { get; set; } = new List<TargetGroup>();
        public ICollection<Run> Runs { get; set; } = new List<Run>();
        public ICollection<Lead> Leads { get; set; } = new List<Lead>();
    }
}
