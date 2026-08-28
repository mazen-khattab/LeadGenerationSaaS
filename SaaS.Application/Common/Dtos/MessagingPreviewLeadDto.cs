using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Dtos
{
    public record MessagingPreviewLeadDto
    {
        public long LeadId { get; set; }
        public string LeadNumber { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;
        public string ProfileUrl { get; set; } = string.Empty;
        public string AiMessage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
