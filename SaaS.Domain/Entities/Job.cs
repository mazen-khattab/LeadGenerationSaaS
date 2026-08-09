using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Domain.Entities
{
    public class Job
    {
        public long Id { get; set; }
        public string Type { get; set; } = string.Empty; // e.g. "ScrapeGroup", "SendDm"
        public int? BotId { get; set; }
        public int RunId { get; set; }
        public string PayloadJson { get; set; } = "{}";

        // Status: "Pending", "Processing", "Completed", "Failed"
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public Run Run { get; set; } = null!;
    }
}
