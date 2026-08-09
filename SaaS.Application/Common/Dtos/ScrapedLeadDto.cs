using System;

namespace SaaS.Application.Common.Dtos
{
    public record ScrapedLeadDto(string ExternalId, string Username, string FullName, string AiMessage, string MetadataJson);
}
