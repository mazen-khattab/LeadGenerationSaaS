using System;

namespace SaaS.Application.Common.Dtos
{
    public record LeadListDto
    {
        public long LeadId { get; set; }
        public string LeadFormattedId { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;
        public string ProfileUrl { get; set; } = string.Empty;
        public string AiMessage { get; set; } = string.Empty;
        public string FromGroup { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
