namespace SaaS.Application.Common.Dtos
{
    public record DispatchMessagingResultDto(long JobId, string Status, int TotalLeadsCount);
}
