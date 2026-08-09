using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Domain.Entities
{
    public class LeadDetail
    {
        public long LeadId { get; set; } // Primary & Foreign Key
        public string MetaDataJson { get; set; } = "{}"; // Bios, comments, post context

        // Navigation Property
        public Lead Lead { get; set; } = null!;
    }
}
