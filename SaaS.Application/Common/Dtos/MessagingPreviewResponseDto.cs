using SaaS.Application.Common.Models;

namespace SaaS.Application.Common.Dtos
{
    public record MessagingPreviewResponseDto
    {
        public int DailyMessageLimit { get; set; }
        public int MessagesSent { get; set; }
        public int AllowedToProcessCount { get; set; }
        public List<MessagingPreviewLeadDto> Leads { get; set; } = new List<MessagingPreviewLeadDto>();
    }
}
