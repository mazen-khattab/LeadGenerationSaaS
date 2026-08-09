namespace SaaS.Application.Common.Dtos
{
    public record AddGroupDto(int BotId, string GroupName, string GroupUrl, string? ConfigJson, bool IsActive);
}
