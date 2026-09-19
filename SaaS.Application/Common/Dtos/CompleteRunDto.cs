using System.Collections.Generic;

namespace SaaS.Application.Common.Dtos
{
    public record CompleteRunDto(List<ScrapedLeadDto> ExtractedLeads);
}
